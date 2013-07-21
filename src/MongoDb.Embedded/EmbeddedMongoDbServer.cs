using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MongoDB.Driver;

namespace MongoDB.Embedded
{
    public class EmbeddedMongoDbServer : IDisposable
    {
        private Process _process;
        private readonly int _port;
        private readonly string _path;
        private readonly string _name;
        private readonly int _processEndTimeout;
        private readonly ManualResetEventSlim _gate = new ManualResetEventSlim(false);

        public EmbeddedMongoDbServer()
        {
            _port = GetRandomUnusedPort();

            _processEndTimeout = 10000;

            KillMongoDbProcesses(_processEndTimeout);

            _name = RandomFileName(7);
            _path = Path.Combine(Path.GetTempPath(), RandomFileName(12));
            Directory.CreateDirectory(_path);

            using (var resourceStream = typeof(EmbeddedMongoDbServer).Assembly.GetManifestResourceStream(typeof(EmbeddedMongoDbServer), "mongod.exe"))
            using (var fileStream = new FileStream(Path.Combine(_path, _name + ".exe"), FileMode.Create, FileAccess.Write))
            {
                resourceStream.CopyTo(fileStream);
            }

            _process = new Process
            {
                StartInfo =
                {
                    Arguments = string.Format("--dbpath \"{0}\" --logappend --bind_ip 127.0.0.1 --port {1}", _path, _port),
                    UseShellExecute = false,
                    ErrorDialog = false,
                    LoadUserProfile = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    FileName = Path.Combine(_path, _name + ".exe"),
                    WorkingDirectory = _path
                }
            };

            _process.OutputDataReceived += ProcessOutputDataReceived;
            _process.ErrorDataReceived += ProcessErrorDataReceived;
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            
            _gate.Wait(10000);
        }

        public MongoClientSettings Settings
        {
            get { return new MongoClientSettings { Server = new MongoServerAddress("127.0.0.1", _port) }; }
        }

        public MongoClient Client
        {
            get { return new MongoClient(Settings); }
        }

        private static string RandomFileName(int length)
        {
            var chars = "abcdefghijklmnopqrstuvwxyz1234567890".ToCharArray();
            var data = new byte[1];
            var crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            data = new byte[length];
            crypto.GetNonZeroBytes(data);
            var result = new StringBuilder(length);
            foreach (byte b in data)
                result.Append(chars[b % chars.Length]);

            return result.ToString();
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        protected void Dispose(bool disposing)
        {
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        _process.WaitForExit(_processEndTimeout);
                    }

                    _process.Dispose();
                }
                catch (Exception exception)
                {
                    Trace.TraceWarning(string.Format("Got exception when disposing the mongod server process msg = {0}", exception.Message));
                }

                _process = null;
            }

            if (Directory.Exists(_path))
                Directory.Delete(_path, true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~EmbeddedMongoDbServer()
        {
            Dispose(false);
        }

        private void KillMongoDbProcesses(int millisTimeout)
        {
            var processesByName = Process.GetProcessesByName(_name);
            foreach (var process in processesByName)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(millisTimeout);
                    }
                }
                catch (Exception exception)
                {
                    Trace.TraceWarning(string.Format("Got exception when killing mongod.exe msg = {0}", exception.Message));
                }
            }
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                Trace.WriteLine(string.Format("Err - {0}", e.Data));
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.Contains("waiting for connections on port " + _port))
                    _gate.Set();

                Trace.WriteLine(string.Format("Output - {0}", e.Data));
            }
        }
    }
}

