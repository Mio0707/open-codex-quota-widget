using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CodexQuotaIsland.Audio;

internal sealed class WasapiLoopbackAnalyzer : IDisposable
{
	private readonly object _stateLock = new();
	private Thread? _thread;
	private volatile bool _running;
	private volatile bool _connected;
	private volatile float _overall;
	private volatile float _bass;
	private volatile float _mid;
	private volatile float _treble;
	private int _beatFlag;
	private double _lowState;
	private double _wideState;
	private double _bassAverage = 0.015;
	private long _lastBeatTicks;
	private byte[] _sampleBuffer = Array.Empty<byte>();

	public float Overall => _overall;
	public float Bass => _bass;
	public float Mid => _mid;
	public float Treble => _treble;
	public bool IsConnected => _connected;

	public void Start()
	{
		lock (_stateLock)
		{
			if (_running) return;
			_running = true;
			_thread = new Thread(CaptureLoop) { IsBackground = true, Name = "System audio visualizer" };
			_thread.Start();
		}
	}

	public void Stop()
	{
		Thread? thread;
		lock (_stateLock)
		{
			_running = false;
			thread = _thread;
			_thread = null;
		}
		if (thread is { IsAlive: true }) thread.Join(1300);
		_connected = false;
	}

	public bool ConsumeBeat() => Interlocked.Exchange(ref _beatFlag, 0) == 1;

	private void CaptureLoop()
	{
		NativeMethods.CoInitializeEx(IntPtr.Zero, 0);
		try
		{
			while (_running)
			{
				try { CaptureCurrentDevice(); }
				catch
				{
					_connected = false;
					DecayLevels();
					if (_running) Thread.Sleep(900);
				}
			}
		}
		finally { NativeMethods.CoUninitialize(); }
	}

	private void CaptureCurrentDevice()
	{
		IMMDeviceEnumerator? enumerator = null;
		IMMDevice? device = null;
		IAudioClient? client = null;
		IAudioCaptureClient? capture = null;
		IntPtr formatPointer = IntPtr.Zero;
		string? deviceId = null;
		try
		{
			enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
			Check(enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out device));
			Check(device.GetId(out deviceId));
			Guid audioClientId = typeof(IAudioClient).GUID;
			Check(device.Activate(ref audioClientId, 23, IntPtr.Zero, out object clientObject));
			client = (IAudioClient)clientObject;
			Check(client.GetMixFormat(out formatPointer));
			WaveFormatInfo format = WaveFormatInfo.FromPointer(formatPointer);
			Guid session = Guid.Empty;
			Check(client.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.Loopback, 2_000_000, 0, formatPointer, ref session));
			Guid captureId = typeof(IAudioCaptureClient).GUID;
			Check(client.GetService(ref captureId, out object captureObject));
			capture = (IAudioCaptureClient)captureObject;
			Check(client.Start());
			_connected = true;

			DateTime nextDeviceCheck = DateTime.UtcNow.AddSeconds(2);
			while (_running)
			{
				Check(capture.GetNextPacketSize(out uint packetFrames));
				if (packetFrames == 0)
				{
					DecayLevels();
					Thread.Sleep(8);
				}
				else
				{
					while (packetFrames > 0)
					{
						Check(capture.GetBuffer(out IntPtr data, out uint frames, out AudioClientBufferFlags flags, out _, out _));
						try
						{
							if ((flags & AudioClientBufferFlags.Silent) != 0) DecayLevels();
							else ProcessPacket(data, frames, format);
						}
						finally { Check(capture.ReleaseBuffer(frames)); }
						Check(capture.GetNextPacketSize(out packetFrames));
					}
				}

				if (DateTime.UtcNow >= nextDeviceCheck)
				{
					IMMDevice? latest = null;
					try
					{
						Check(enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out latest));
						Check(latest.GetId(out string latestId));
						if (!string.Equals(deviceId, latestId, StringComparison.OrdinalIgnoreCase)) break;
					}
					finally { ReleaseCom(latest); }
					nextDeviceCheck = DateTime.UtcNow.AddSeconds(2);
				}
			}
		}
		finally
		{
			_connected = false;
			if (client is not null) try { client.Stop(); } catch { }
			if (formatPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(formatPointer);
			ReleaseCom(capture);
			ReleaseCom(client);
			ReleaseCom(device);
			ReleaseCom(enumerator);
		}
	}

	private void ProcessPacket(IntPtr pointer, uint frameCount, WaveFormatInfo format)
	{
		int byteCount = checked((int)frameCount * format.BlockAlign);
		if (_sampleBuffer.Length < byteCount) _sampleBuffer = new byte[Math.Max(byteCount, _sampleBuffer.Length * 2)];
		Marshal.Copy(pointer, _sampleBuffer, 0, byteCount);

		double overallEnergy = 0, bassEnergy = 0, midEnergy = 0, trebleEnergy = 0;
		double lowAlpha = Math.Exp(-2.0 * Math.PI * 180.0 / format.SampleRate);
		double wideAlpha = Math.Exp(-2.0 * Math.PI * 2600.0 / format.SampleRate);
		int frames = (int)frameCount;
		int step = Math.Max(1, frames / 2048);
		int sampleCount = 0;
		for (int frame = 0; frame < frames; frame += step)
		{
			int baseOffset = frame * format.BlockAlign;
			double mono = 0;
			for (int channel = 0; channel < format.Channels; channel++)
				mono += format.ReadSample(_sampleBuffer, baseOffset + channel * format.BytesPerSample);
			mono /= format.Channels;
			_lowState = (1.0 - lowAlpha) * mono + lowAlpha * _lowState;
			_wideState = (1.0 - wideAlpha) * mono + wideAlpha * _wideState;
			double low = _lowState, mid = _wideState - _lowState, high = mono - _wideState;
			overallEnergy += mono * mono;
			bassEnergy += low * low;
			midEnergy += mid * mid;
			trebleEnergy += high * high;
			sampleCount++;
		}
		if (sampleCount == 0) return;

		double overallRms = Math.Sqrt(overallEnergy / sampleCount);
		double bassRms = Math.Sqrt(bassEnergy / sampleCount);
		double midRms = Math.Sqrt(midEnergy / sampleCount);
		double trebleRms = Math.Sqrt(trebleEnergy / sampleCount);
		_overall = Smooth(_overall, Normalize(overallRms, 4.2), 0.34f);
		_bass = Smooth(_bass, Normalize(bassRms, 8.0), 0.40f);
		_mid = Smooth(_mid, Normalize(midRms, 8.5), 0.28f);
		_treble = Smooth(_treble, Normalize(trebleRms, 13.0), 0.34f);

		_bassAverage = _bassAverage * 0.96 + bassRms * 0.04;
		long nowTicks = Stopwatch.GetTimestamp();
		double sinceBeat = (nowTicks - _lastBeatTicks) / (double)Stopwatch.Frequency;
		if (bassRms > 0.012 && bassRms > _bassAverage * 1.46 && sinceBeat > 0.18)
		{
			_lastBeatTicks = nowTicks;
			Interlocked.Exchange(ref _beatFlag, 1);
		}
	}

	private void DecayLevels()
	{
		_overall *= 0.90f;
		_bass *= 0.88f;
		_mid *= 0.90f;
		_treble *= 0.88f;
	}

	private static float Normalize(double value, double gain) => (float)Math.Clamp(Math.Pow(Math.Max(0, value * gain), 0.72), 0, 1);
	private static float Smooth(float current, float target, float amount) => current + (target - current) * amount;
	private static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }
	private static void ReleaseCom(object? value)
	{
		if (value is not null && Marshal.IsComObject(value)) try { Marshal.ReleaseComObject(value); } catch { }
	}
	public void Dispose() => Stop();
}

internal sealed class WaveFormatInfo
{
	public int Channels { get; private init; }
	public int SampleRate { get; private init; }
	public int BlockAlign { get; private init; }
	public int BitsPerSample { get; private init; }
	public bool IsFloat { get; private init; }
	public int BytesPerSample => BitsPerSample / 8;

	public static WaveFormatInfo FromPointer(IntPtr pointer)
	{
		WaveFormatEx native = Marshal.PtrToStructure<WaveFormatEx>(pointer);
		bool isFloat = native.FormatTag == 3;
		if (native.FormatTag == 0xFFFE && native.ExtraSize >= 22)
		{
			Guid subFormat = Marshal.PtrToStructure<Guid>(IntPtr.Add(pointer, 24));
			isFloat = subFormat == new Guid("00000003-0000-0010-8000-00aa00389b71");
		}
		if (native.Channels <= 0 || native.SamplesPerSec <= 0 || native.BlockAlign <= 0)
			throw new InvalidOperationException("无法识别当前声音输出格式。");
		return new WaveFormatInfo
		{
			Channels = native.Channels,
			SampleRate = native.SamplesPerSec,
			BlockAlign = native.BlockAlign,
			BitsPerSample = native.BitsPerSample,
			IsFloat = isFloat
		};
	}

	public double ReadSample(byte[] data, int offset)
	{
		if (IsFloat && BitsPerSample == 32) return BitConverter.ToSingle(data, offset);
		if (IsFloat && BitsPerSample == 64) return BitConverter.ToDouble(data, offset);
		if (BitsPerSample == 16) return BitConverter.ToInt16(data, offset) / 32768.0;
		if (BitsPerSample == 24)
		{
			int sample = data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
			if ((sample & 0x800000) != 0) sample |= unchecked((int)0xFF000000);
			return sample / 8388608.0;
		}
		if (BitsPerSample == 32) return BitConverter.ToInt32(data, offset) / 2147483648.0;
		return 0;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct WaveFormatEx
{
	public ushort FormatTag;
	public ushort Channels;
	public int SamplesPerSec;
	public int AverageBytesPerSec;
	public ushort BlockAlign;
	public ushort BitsPerSample;
	public ushort ExtraSize;
}

internal enum EDataFlow { Render, Capture, All }
internal enum ERole { Console, Multimedia, Communications }
internal enum AudioClientShareMode { Shared, Exclusive }
[Flags] internal enum AudioClientStreamFlags : uint { Loopback = 0x00020000 }
[Flags] internal enum AudioClientBufferFlags : uint { Silent = 0x2 }

[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumerator { }

[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
	[PreserveSig] int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IntPtr devices);
	[PreserveSig] int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
	[PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
	[PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
	[PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
	[PreserveSig] int Activate(ref Guid id, uint classContext, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
	[PreserveSig] int OpenPropertyStore(uint access, out IntPtr properties);
	[PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
	[PreserveSig] int GetState(out uint state);
}

[ComImport, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioClient
{
	[PreserveSig] int Initialize(AudioClientShareMode shareMode, AudioClientStreamFlags streamFlags, long bufferDuration, long periodicity, IntPtr format, ref Guid sessionGuid);
	[PreserveSig] int GetBufferSize(out uint bufferFrames);
	[PreserveSig] int GetStreamLatency(out long latency);
	[PreserveSig] int GetCurrentPadding(out uint paddingFrames);
	[PreserveSig] int IsFormatSupported(AudioClientShareMode shareMode, IntPtr format, out IntPtr closestMatch);
	[PreserveSig] int GetMixFormat(out IntPtr deviceFormat);
	[PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
	[PreserveSig] int Start();
	[PreserveSig] int Stop();
	[PreserveSig] int Reset();
	[PreserveSig] int SetEventHandle(IntPtr eventHandle);
	[PreserveSig] int GetService(ref Guid serviceId, [MarshalAs(UnmanagedType.IUnknown)] out object service);
}

[ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioCaptureClient
{
	[PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out AudioClientBufferFlags flags, out ulong devicePosition, out ulong qpcPosition);
	[PreserveSig] int ReleaseBuffer(uint frames);
	[PreserveSig] int GetNextPacketSize(out uint frames);
}

internal static class NativeMethods
{
	[DllImport("ole32.dll")] internal static extern int CoInitializeEx(IntPtr reserved, uint coInit);
	[DllImport("ole32.dll")] internal static extern void CoUninitialize();
}
