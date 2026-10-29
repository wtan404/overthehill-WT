using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace OverTheHillVR.Telemetry
{
    public static class TelemetryBroadcaster
    {
        public static bool Enabled = true;
        public static string TargetIP = "127.0.0.1";
        public static int TargetPort = 5606;
        public static float BroadcastInterval = 1f / 60f; // 60 Hz

        private static UdpClient _udp;
        private static IPEndPoint _endPoint;
        private static float _lastSendTime;
        private static readonly byte[] _buffer = new byte[324];
        private static ForzaPacket _packet;
        private static bool _initialized;
        private static bool _wasDriving;
        private static int _errorCount;

        public static void Init(string ip = null, int port = -1)
        {
            try
            {
                LoadConfig();

                if (!string.IsNullOrEmpty(ip)) TargetIP = ip;
                if (port > 0) TargetPort = port;

                if (!Enabled)
                {
                    Console.WriteLine("[SimHubTelemetry] Telemetry disabled in configuration.");
                    _initialized = true;
                    return;
                }

                if (_udp != null)
                {
                    try { _udp.Close(); _udp.Dispose(); } catch {}
                    _udp = null;
                }

                _endPoint = new IPEndPoint(IPAddress.Parse(TargetIP), TargetPort);
                _udp = new UdpClient();
                _udp.Client.SendBufferSize = 1024;
                _udp.Client.Blocking = false;
                _initialized = true;
                _errorCount = 0;
                Console.WriteLine($"[SimHubTelemetry] Initialized UDP broadcaster targeting {TargetIP}:{TargetPort} @ {(1f / BroadcastInterval):0}Hz");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimHubTelemetry] Initialization error: {ex.Message}");
                _initialized = false;
            }
        }

        private static void LoadConfig()
        {
            try
            {
                string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "MelonPreferences.cfg");
                if (!File.Exists(cfgPath)) cfgPath = "UserData/MelonPreferences.cfg";
                if (!File.Exists(cfgPath)) return;

                bool inSection = false;
                foreach (string line in File.ReadAllLines(cfgPath))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("[SimHubTelemetry]", StringComparison.OrdinalIgnoreCase))
                    {
                        inSection = true;
                        continue;
                    }
                    if (trimmed.StartsWith("[") && inSection)
                    {
                        break;
                    }
                    if (inSection && trimmed.Contains("="))
                    {
                        string[] parts = trimmed.Split(new[] { '=' }, 2);
                        string key = parts[0].Trim();
                        string val = parts[1].Trim().Trim('"');

                        if (key.Equals("Enable", StringComparison.OrdinalIgnoreCase))
                        {
                            Enabled = !val.Equals("false", StringComparison.OrdinalIgnoreCase);
                        }
                        else if (key.Equals("TargetIP", StringComparison.OrdinalIgnoreCase))
                        {
                            TargetIP = val;
                        }
                        else if (key.Equals("TargetPort", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out int p))
                        {
                            TargetPort = p;
                        }
                        else if (key.Equals("BroadcastRateHz", StringComparison.OrdinalIgnoreCase) && float.TryParse(val, out float hz) && hz > 0)
                        {
                            BroadcastInterval = 1f / hz;
                        }
                    }
                }
            }
            catch {}
        }

        public static void TickSimple(
            bool isDriving,
            float timeNow,
            float rpm,
            float speed,
            int gear,
            float accelY,
            float ffbHit,
            float steer)
        {
            if (!_initialized)
            {
                Init();
            }

            if (!Enabled) return;

            if (!isDriving)
            {
                if (_wasDriving)
                {
                    SendZeroPacket();
                    _wasDriving = false;
                }
                return;
            }

            _wasDriving = true;

            if (timeNow - _lastSendTime < BroadcastInterval)
            {
                return;
            }
            _lastSendTime = timeNow;

            try
            {
                _packet.IsRaceOn = 1;
                _packet.TimestampMS = (uint)(timeNow * 1000f);
                _packet.EngineMaxRpm = 7500f;
                _packet.EngineIdleRpm = 850f;
                _packet.CurrentEngineRpm = MathF.Max(0f, rpm);

                // G-Forces (Y = vertical bumps, X = lateral, Z = longitudinal)
                float hitSpike = (MathF.Abs(ffbHit) > 0.05f) ? (ffbHit * 25f) : 0f;
                _packet.AccelerationY = accelY + hitSpike;
                _packet.AccelerationX = steer * speed * 0.15f;
                _packet.AccelerationZ = 0f;

                // Speed & Velocities
                _packet.Speed = MathF.Max(0f, speed);
                _packet.VelocityZ = MathF.Max(0f, speed);
                _packet.Power = 150000f;
                _packet.Torque = 300f;

                // Gear: 0 = Reverse, 0 = Neutral, 1..N = 1st..Nth
                byte fzGear = 0;
                if (gear > 0) fzGear = (byte)gear;
                else if (gear < 0) fzGear = 0;
                else fzGear = 0;
                _packet.Gear = fzGear;

                // Suspension & Surface Rumble (drives road vibrations / bumps)
                float bumpIntensity = Math.Clamp(MathF.Abs(accelY) / 12f, 0f, 1f);
                _packet.NormalizedSuspensionTravelFrontLeft = 0.5f;
                _packet.NormalizedSuspensionTravelFrontRight = 0.5f;
                _packet.NormalizedSuspensionTravelRearLeft = 0.5f;
                _packet.NormalizedSuspensionTravelRearRight = 0.5f;

                _packet.SurfaceRumbleFrontLeft = bumpIntensity;
                _packet.SurfaceRumbleFrontRight = bumpIntensity;
                _packet.SurfaceRumbleRearLeft = bumpIntensity;
                _packet.SurfaceRumbleRearRight = bumpIntensity;

                // Steer (-127 to +127)
                _packet.Steer = (sbyte)(Math.Clamp(steer, -1f, 1f) * 127f);

                // Fast pointer copy to byte buffer (zero garbage allocation)
                unsafe
                {
                    fixed (byte* pBuf = _buffer)
                    {
                        *(ForzaPacket*)pBuf = _packet;
                    }
                }

                _udp.Send(_buffer, 324, _endPoint);
            }
            catch (Exception)
            {
                if (++_errorCount <= 3)
                {
                    Console.WriteLine("[SimHubTelemetry] UDP socket send error (muted after 3 occurrences)");
                }
            }
        }

        private static void SendZeroPacket()
        {
            try
            {
                if (_udp == null || _endPoint == null) return;
                Array.Clear(_buffer, 0, _buffer.Length);
                _udp.Send(_buffer, 324, _endPoint);
            }
            catch {}
        }

        public static void Shutdown()
        {
            try
            {
                SendZeroPacket();
                if (_udp != null)
                {
                    _udp.Close();
                    _udp.Dispose();
                    _udp = null;
                }
                _initialized = false;
            }
            catch {}
        }
    }
}
