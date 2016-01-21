using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using System.Xml;
using System.IO;

using System.Runtime.Serialization.Formatters.Binary;
using DataClass;
using SDK_SC_RfidReader;
using SDK_SC_Fingerprint;

namespace TcpIP_class
{
    /// <summary>
    /// Frame object, passed as a State for AsyncResult class (see .NET Asynchronous TCP).
    /// </summary>
    public class StateObject
    {
        public Socket WorkSocket = null;
        public byte[] Buffer = new byte[BufferSize];
        public const int BufferSize = 4194304;
        public StringBuilder Sb = new StringBuilder();


        public StateObject(Socket clientSocket)
        {
            WorkSocket = clientSocket;
        }
    }

    /// <summary>
    /// TcpClient class handle asynchronous communication (through TCP/IP) with a remote device.
    /// </summary>
    public class TcpArmClient
    {
        public const char DELIMITER = (char) 0x1C;
	    public const char END_OF_MESSAGE = (char) 0x04;

        private Socket _client;
        private readonly IPEndPoint _remoteEP;
        private int _connectionTry;

		private TcpArmDevice _device;

		private readonly Dictionary<String, Queue<AutoResetEvent>> _requestCodeToEvent = new Dictionary<String, Queue<AutoResetEvent>>();
		private string[] _syncResponsePackets = {};
 
        /// <summary>Default constructor./summary>
        /// <param name="ipAddress">IP address of the server</param>
        /// <param name="port">Port used for communication</param>
        /// <param name="device">TcpDevice to be informed when any event is received</param>
        public TcpArmClient(string ipAddress, int port, TcpArmDevice device)
		{
			IPAddress[] ipAddresses = Dns.GetHostAddresses(ipAddress); 
			IPAddress ipTest = null;
 
			foreach(IPAddress ip in ipAddresses) 
			{
				if(ip.AddressFamily == AddressFamily.InterNetwork)
				{
					ipTest = ip;
				}
			}

			if(ipTest != null) 
			{
				_remoteEP = new IPEndPoint(ipTest, port);
			}

			_device = device;
        }

        public bool IsConnected()
        {
            try
            {
                return !(_client.Poll(1, SelectMode.SelectRead) && _client.Available == 0 && _client.Connected);
            }
            catch (SocketException) { return false; }
        }

        public string getLocalEndPoint()
        {
            return _client.LocalEndPoint.ToString();
        }


        /// <summary>Try to create and connect the TCP socket.</summary>
        public bool Start()
		{
			if(_remoteEP == null) 
			{
				return false;
			}

            try
            {
				_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _client.BeginConnect(_remoteEP, (ConnectCallback), _client);
				return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                Stop();
				return false;
            }
        } 
 
		/// <summary>Called once the socket is successfully connected.</summary>
		/// <param name='ar'>Async result provided by the callback.</param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
                if ((_device != null) && (_device.ConnectedEvent != null))
				    _device.ConnectedEvent.Set();
 
				Receive();
            } catch(SocketException)
            {
                if(_connectionTry < 4) // try to reconnect 3 times, max
				{
                    Start();
				}

                ++_connectionTry;
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
 
		/// <summary>Send a blocking request with 1500ms (default) of timeout delay.</summary>
		/// <returns>String array containing the response packets (or empty if request timed out).</returns>
		/// <param name='requestCode'><see cref="Client.RequestCode"/> value.</param>
		/// <param name='parameters'>Additional (optional) parameters.</param>
        public string[] SendSynchronousRequest(string requestCode, params string[] parameters)
		{
			return SendSynchronousRequest(1500, requestCode, parameters);
        } 
		
		/// <summary>Send a blocking request with given timeout delay.</summary>
		/// <returns>String array containing the response packets (or empty if request timed out).</returns>
		/// <param name='timeoutMs'>Timeout delay (in milliseconds).</param>
		/// <param name='requestCode'><see cref="Client.RequestCode"/> value.</param>
		/// <param name='parameters'>Additional (optional) parameters.</param>
		public string[] SendSynchronousRequest (int timeoutMs, string requestCode, params string[] parameters)
		{
			var blockingEvent = new AutoResetEvent (false);

			if (!_requestCodeToEvent.ContainsKey(requestCode))
			{
				_requestCodeToEvent[requestCode] = new Queue<AutoResetEvent>();
			}

			_requestCodeToEvent[requestCode].Enqueue(blockingEvent);

			Send(requestCode, parameters);
 
			if(blockingEvent.WaitOne(timeoutMs)) 
			{
				return _syncResponsePackets;
			}

			return new string[0];
		}
 
		/// <summary>Initialize incoming-data buffer and start asynchronously receiving.</summary>
        private void Receive()
        {
            try
            {
                StateObject state = new StateObject(_client); 
                _client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, (ReceiveCallback), state);
            }
            catch(Exception e)
            {
                // Socket has been closed/disposed
                if(e is ObjectDisposedException || e is SocketException)
                {
                    
                    Console.WriteLine("[1] Connection with server aborted.");
					// TODO: Raise DeviceDisconnected event
                    _device.RaiseDisconnectEvent();
                    return;
                }
 
                // else : unexpected exception !
                throw;
            }
        } 
 
        /// <summary>
		/// Incoming-data reception callback. 
		/// If the message received is an EventCode: forward the EventCode value to the TcpDevice. 
		/// Else, consider it as a response to a blocking (or "synchronous") request and unlock the waiting thread.
		/// </summary>
        /// <param name="ar">Callback async result</param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
 
                int bytesRead = _client.EndReceive(ar);
 
                if(bytesRead == 0)
                {
					return;
                }

				// put new data in the buffer
				state.Sb.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));
                string response = state.Sb.ToString();

				// if we received a complete frame (or many)
                if(response[response.Length - 1] == END_OF_MESSAGE)
                {	
					// reset the current StringBuilder
					state.Sb = new StringBuilder();

					// handle the response(s)
                    string[] responses = response.Split(new[] { END_OF_MESSAGE }, 
											StringSplitOptions.RemoveEmptyEntries);

                    foreach(string currentResponse in responses)
                    {
			            string[] packets = currentResponse.Split(new[] { DELIMITER }, 
							StringSplitOptions.RemoveEmptyEntries);

						if(packets.Length == 0)
						{
							continue;
						}

                        if(packets[0].StartsWith("event_"))
						{
                            _device.HandleEvent(packets);
						}

						else
						{
							if(!_requestCodeToEvent.ContainsKey(packets[0]))
							{
								continue;
							}

							_syncResponsePackets = packets;
							var blockingEvent = _requestCodeToEvent[packets[0]].Dequeue();
                            if (blockingEvent != null)
							    blockingEvent.Set();
						}
                    }
                }

				// keep listening
                _client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,(ReceiveCallback), state);

            } catch(Exception e)
            { 
                // Socket has been closed/disposed after the user decided to exit (with "exit" command).
                if(e is ObjectDisposedException || e is SocketException)
                {
                    Console.WriteLine("[2] Connection with server aborted.");
					// TODO: Raise DeviceDisconnected event
                    _device.RaiseDisconnectEvent();
                    return;
                }
 
                // else : unexpected exception !
                throw;
            }
        } 
 
        /// <summary>Send a message to the Server (<see cref="Client.RequestCode"/> + parameters (if any))./summary>
        /// <param name="requestCode"><see cref="Client.RequestCode"/> value.</param>
        /// <param name="parameters">Parameters provided with the <see cref="Client.RequestCode"/> value.</param>
        public void Send(string requestCode, params string[] parameters)
		{
			var sb = new StringBuilder();

			sb.Append(requestCode);

			foreach(string param in parameters) 
			{
				sb.Append(DELIMITER);
				sb.Append(param);
			}

			sb.Append(END_OF_MESSAGE);

			byte[] byteData = Encoding.ASCII.GetBytes(sb.ToString());
 
			try
			{
				_client.BeginSend(byteData, 0, byteData.Length, 0, (SendCallback), _client);
			} catch(Exception e) 
			{				
                // Socket has been closed/disposed
                if(e is ObjectDisposedException || e is SocketException)
                {
                    Console.WriteLine("[3] Connection with server aborted.");
                    _device.RaiseDisconnectEvent();
                    // TODO: Raise DeviceDisconnected event
                    return;
                }
 
                // else : unexpected exception !
                throw;
			}
        } 
 
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndSend(ar);
            }
            catch (Exception e) { }
        }
 
        /// <summary>
        /// Shutdown the socket and close it.
        /// </summary>
        public void Stop()
        {
            _client.Shutdown(SocketShutdown.Both);
            _client.Close(500);
        }
    }

    /// <summary>
    /// Network (Asynchronous TCP/IP) implementation of RfidDevice class to communicate with SmartServer.
    /// </summary>
    public class TcpArmDevice
    {
        /// <summary>
        /// Used to raise RFID device events (Scan started, tag added, etc.) according to Server's response.
        /// </summary>
        /// <param name="args">rfidReaderArgs</param>
        public delegate void DeviceEventHandler(rfidReaderArgs args);
        public event DeviceEventHandler DeviceEvent;
        public readonly AutoResetEvent ConnectedEvent = new AutoResetEvent(false);

        // used to force the "synchronousness" of the lighting operation, just like in the C# SDK
        private readonly AutoResetEvent _lightingStartedEvent = new AutoResetEvent(false);
        // list of tags that could not be lighted at the last lighting operation (see Lighting Started event)
        private List<String> _tagsNotLighted = new List<string>();

        private TcpArmClient _tcpClient;

        private string _serialNumber;
        private DeviceType _deviceType = DeviceType.DT_UNKNOWN;
        private DeviceStatus _deviceStatus = DeviceStatus.DS_NotReady;
        private string _hardwareVersion;
        private string _softwareVersion;
        private string _lastBadgeScanned = String.Empty;
        public const double TEMPERATURE_ERROR = -777;

        public int CptTag { get; set; }
        public int ScanId { get; set; }
    
        /// <summary>Initializes a new instance of the <see cref="Client.TcpDevice"/> class.</summary>
        /// <param name='ipAddress'>
        /// Ip address.
        /// </param>
        public TcpArmDevice(string ipAddress)
        {
            _tcpClient = new TcpArmClient(ipAddress, 8080, this);

            if (!_tcpClient.Start())
            {
                throw new Exception("Unable to establish a connection with the remote device.");
            }

            if (ConnectedEvent.WaitOne(2000))
            {
                if (!processInitialization())
                {
                    throw new Exception("Unable to get basic information from remote SpaceCode device.");
                }

                Console.WriteLine("Connected to " + _deviceType + " (" + _serialNumber + ") on " + ipAddress + ":8080");
                return;
            }

            throw new Exception("Connection to the remote device failed.");
        }

        /// <summary>Initializes a new instance of the <see cref="Client.TcpDevice"/> class with port.</summary>
        /// <param name='ipAddress'>
        /// Ip address.
        /// </param>
        /// <param name='port'>
        /// Port.
        /// </param>
        public TcpArmDevice(string ipAddress,int port)
        {
            _tcpClient = new TcpArmClient(ipAddress, port, this);

            if (!_tcpClient.Start())
            {
                throw new Exception("Unable to establish a connection with the remote device.");
            }

            if (ConnectedEvent.WaitOne(2000))
            {
                if (!processInitialization())
                {
                    throw new Exception("Unable to get basic information from remote SpaceCode device.");
                }

                Console.WriteLine("Connected to " + _deviceType + " (" + _serialNumber + ") on " + ipAddress + ": " +port);
                return;
            }

            throw new Exception("Connection to the remote device failed.");
        }


        /// <summary>
        /// Gets Device connected to Server or not
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsDeviceConnected()
        {
            return _tcpClient.IsConnected();
        }

        public string getLocalEndPoint()
        {
            return _tcpClient.getLocalEndPoint();
        }

        /// <summary>Send a <see cref="Client.RequestCode"/>.DISCONNECT and end TCP socket connection.</summary>
        public void Release()
        {
            try
            {
                _tcpClient.Send(RequestCode.DISCONNECT);
                _tcpClient.Stop();
            }
            catch(Exception exp){

            }
        }

        public void RaiseDisconnectEvent()
        {
            try
            {
                rfidReaderArgs args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_Disconnected, String.Empty);
                RaiseEvent(args);
            }
            catch (Exception exp)
            {

            }
        }

        /// <summary>Ask the remote device to provide its serial number, type, status, hardware and software versions.</summary>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public bool processInitialization()
        {
            string[] packets = _tcpClient.SendSynchronousRequest(RequestCode.INITIALIZATION);

            if (packets.Length < 5 || RequestCode.INITIALIZATION != packets[0])
            {
                return false;
            }

            _serialNumber = packets[1];
            _hardwareVersion = packets[3];
            _softwareVersion = packets[4];
            ScanId = 0;

            switch (packets[2])
            {
                case "SAS":
                    _deviceType = DeviceType.DT_SAS;
                    break;

                case "SMARTBOARD":
                    _deviceType = DeviceType.DT_SBR;
                    break;

                case "SMARTBOX":
                    _deviceType = DeviceType.DT_SBX;
                    break;

                case "SMARTCABINET":
                    _deviceType = DeviceType.DT_SMC;
                    break;

                case "SMARTDRAWER":
                    _deviceType = DeviceType.DT_MSR;
                    break;

                case "SMARTFRIDGE":
                    _deviceType = DeviceType.DT_SFR;
                    break;

                case "SMARTPAD":
                    // TODO: Update DLL to set SmartPad as DeviceType
                    break;

                case "SMARTRACK":
                    _deviceType = DeviceType.DT_STR;
                    break;

                case "SMARTSTATION":
                    // TODO: Update DLL to set SmartStation as DeviceType
                    break;
            }

            if (packets.Length > 5)
            {
                switch (packets[5])
                {
                    case "DOOR_CLOSED":
                        _deviceStatus = DeviceStatus.DS_DoorClose;
                        break;

                    case "DOOR_OPEN":
                        _deviceStatus = DeviceStatus.DS_DoorOpen;
                        break;

                    case "ERROR":
                        _deviceStatus = DeviceStatus.DS_InError;
                        break;

                    case "FLASHING_FIRMWARE":
                        _deviceStatus = DeviceStatus.DS_FlashFirmware;
                        break;

                    case "LED_ON":
                        _deviceStatus = DeviceStatus.DS_LedOn;
                        break;

                    case "NOT_READY":
                        _deviceStatus = DeviceStatus.DS_NotReady;
                        break;

                    case "SCANNING":
                        _deviceStatus = DeviceStatus.DS_InScan;
                        break;

                    case "READY":
                        _deviceStatus = DeviceStatus.DS_Ready;
                        break;

                    case "WAIT_MODE":
                        _deviceStatus = DeviceStatus.DS_WaitTag;
                        break;
                }
                _deviceStatus = NewToOldEnumStatus(packets[5]);
            }

            return true;
        }
        #region Utility
			/// <summary>Get a DeviceStatus (C#) value matching the provided one (Java)</summary>
			/// <param name='newStatusValue'>Name of a member of the enumeration "DeviceStatus" from the Java SDK</param>
			/// <returns>The equivalent C# version of DeviceStatus [or DeviceStatus.DS_InError if no matching value exists]</returns>
			private static DeviceStatus NewToOldEnumStatus(string newStatusValue)
			{
				switch(newStatusValue)
				{
					case "DOOR_CLOSED":
						return DeviceStatus.DS_DoorClose;
	
					case "DOOR_OPEN":
						return DeviceStatus.DS_DoorOpen;
	
					case "FLASHING_FIRMWARE":
						return DeviceStatus.DS_FlashFirmware;
	
					case "LED_ON":
						return DeviceStatus.DS_LedOn;
	
					case "NOT_READY":
						return DeviceStatus.DS_NotReady;
	
					case "SCANNING":
						return DeviceStatus.DS_InScan;

					case "READY":
						return DeviceStatus.DS_Ready;
	
					case "WAIT_MODE":
						return DeviceStatus.DS_WaitTag;
						
    				case "ERROR":
					default:
						return DeviceStatus.DS_InError;
				}
			} 
			#endregion

        #region Requests
        /// <summary>Ask (through TCP/IP network) to remote device to add a new user.</summary>
        /// <param name='newUser'>User to be added.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public bool AddUser(DeviceGrant newUser)
        {
            string serializedUser = SerializationHelper.SerializeUser(newUser);
            string base64EncodedUser = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedUser));

            if (String.IsNullOrEmpty(base64EncodedUser))
            {
                return false;
            }

            string[] packets = _tcpClient.SendSynchronousRequest(RequestCode.ADD_USER, base64EncodedUser);

            if (packets == null || packets.Length <= 1 || RequestCode.ADD_USER != packets[0])
            {
                return false;
            }

            return packets[1] == "true";
        }

        /// <summary>Sequentially add a list of users to remote device.</summary>
        /// <param name='users'>List of users to be added.</param>
        /// <returns>List of GrantedUser who couldn't be added.</returns>
        public List<DeviceGrant> AddUsers(List<DeviceGrant> users)
        {
            var notAddedUsers = new List<DeviceGrant>();

            foreach (var user in users)
            {
                if (!AddUser(user))
                {
                    notAddedUsers.Add(user);
                }
            }

            return notAddedUsers;
        }

        /// <summary>Ask (through TCP/IP network) to remote device to remove an user.</summary>
        /// <param name='username'>Username of the user who should be removed from granted users list.</param>
        /// <returns>True if operation succeeded, false otherwise.</returns>
        public bool RemoveUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            string[] packets = _tcpClient.SendSynchronousRequest(RequestCode.REMOVE_USER, username);

            if (packets == null || packets.Length <= 1 || RequestCode.REMOVE_USER != packets[0])
            {
                return false;
            }

            return packets[1] == "true";
        }

        /// <summary>Start finger enrollment on master fingerprint reader (on the remote device).</summary>
        /// <param name='username'>User to be enrolled.</param>
        /// <param name='fingerIndex'>Enrolled fingerprint index.</param>
        /// <returns>True if enrollment process succeeded, false otherwise.</returns>
        public bool EnrollFinger(String username, FingerIndexValue fingerIndex, String t)
        {
            if (string.IsNullOrEmpty(username) || fingerIndex == FingerIndexValue.Unknown_Finger)
            {
                return false;
            }

            String[] responsePackets = _tcpClient.SendSynchronousRequest(5 * 60 * 1000,
                    RequestCode.ENROLL_FINGER,
                    username,
                    SerializationHelper.GetSmartServerFingerIndex(fingerIndex).ToString(),
                    "true",
                    t);

            if (responsePackets.Length != 2 || RequestCode.ENROLL_FINGER != responsePackets[0])
            {
                return false;
            }

            return "true" == responsePackets[1];
        }

        /// <summary>Start finger enrollment (on the remote device)..</summary>
        /// <param name='username'>User to be enrolled.</param>
        /// <param name='fingerIndex'>Enrolled fingerprint index.</param>
        /// <param name='useMasterReader'>If true, use the master reader, otherwise use the slave.</param>
        /// <returns>True if enrollment process succeeded, false otherwise.</returns>
        public bool EnrollFinger(String username, FingerIndexValue fingerIndex, bool useMasterReader, String t)
        {
            if (string.IsNullOrEmpty(username) || fingerIndex == FingerIndexValue.Unknown_Finger)
            {
                return false;
            }

            String[] responsePackets = _tcpClient.SendSynchronousRequest(5 * 60 * 1000,
                    RequestCode.ENROLL_FINGER,
                    username,
                     SerializationHelper.GetSmartServerFingerIndex(fingerIndex).ToString(),
                    useMasterReader.ToString(),
                    t);

            if (responsePackets.Length != 2 || RequestCode.ENROLL_FINGER != responsePackets[0])
            {
                return false;
            }

            return "true" == responsePackets[1];
        }

        /// <summary>Ask remote device to remove an user's fingerprint./summary>
        /// <param name='username'>User whose fingerprint has to be removed.</param>
        /// <param name='fingerIndex'>Finger index of the fingerprint which should be removed.</param>
        /// <returns>True if operation succeeded, false otherwise.</returns>
        public bool RemoveFingerprint(String username, FingerIndexValue fingerIndex)
        {
            if (string.IsNullOrEmpty(username) || fingerIndex == FingerIndexValue.Unknown_Finger)
            {
                return false;
            }

            String[] responsePackets = _tcpClient.SendSynchronousRequest(RequestCode.REMOVE_FINGERPRINT,
                    username, SerializationHelper.GetSmartServerFingerIndex(fingerIndex).ToString());

            if (responsePackets.Length != 2 || RequestCode.REMOVE_FINGERPRINT != responsePackets[0])
            {
                return false;
            }

            return "true" == responsePackets[1];
        }

        /// <summary>Ask (through TCP/IP network) to remote device to update an user's permission.</summary>
        /// <param name='username'>User whose permission has to be updated.</param>
        /// <param name='permission'>New permission (UserGrant value).</param>
        /// <returns>True if operation succeeded, false otherwise.</returns>
        public bool UpdatePermission(string username, UserGrant permission)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            string newPermission =
                permission == UserGrant.UG_MASTER_AND_SLAVE
                    ? "ALL"
                    : permission == UserGrant.UG_MASTER
                    ? "MASTER"
                    : permission == UserGrant.UG_SLAVE
                    ? "SLAVE"
                    : "UNDEFINED";

            string[] packets = _tcpClient.SendSynchronousRequest(RequestCode.UPDATE_PERMISSION,
                                                                         username, newPermission);

            if (packets == null || packets.Length < 2 || RequestCode.UPDATE_PERMISSION != packets[0])
            {
                return false;
            }

            return "true" == packets[1];
        }

        /// <summary>Ask the remote device to update an user's badge number.</summary>
        /// <param name='username'>User whose badge number has to be updated.</param>
        /// <param name='badgeNumber'>New badge number (leave empty to remove any badge).</param>
        /// <returns>True if operation succeeded, false otherwise.</returns>
        public bool UpdateBadgeNumber(string username, string badgeNumber)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            string[] packets =
                _tcpClient.SendSynchronousRequest(RequestCode.UPDATE_BADGE, username, badgeNumber);

            if (packets.Length < 2 || RequestCode.UPDATE_BADGE != packets[0])
            {
                return false;
            }

            return "true" == packets[1];
        }

        /// <summary>Provide a DeviceGrant instance of the desired user.</summary>
        /// <param name='username'>Desired user's username.</param>
        /// <returns>A copy of DeviceGrant or null if something went wrong.</returns>
        public DeviceGrant GetUserByName(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            string[] packets = _tcpClient.SendSynchronousRequest(RequestCode.USER_BY_NAME, username);

            if (packets == null || packets.Length < 2 || RequestCode.USER_BY_NAME != packets[0])
            {
                return null;
            }

            string serializedUser;

            try
            {
                serializedUser = Encoding.UTF8.GetString(Convert.FromBase64String(packets[1]));
            }
            catch (Exception)
            {
                return null;
            }

            return SerializationHelper.DeserializeUser(serializedUser,_serialNumber);
        }

        /// <returns>List of users having a permission on the remote device (if any), empty if error.</returns>
        public List<DeviceGrant> GetUsersList()
        {
            var users = new List<DeviceGrant>();

            String[] packets = _tcpClient.SendSynchronousRequest(RequestCode.USERS_LIST);

            if (packets.Length < 2 || RequestCode.USERS_LIST != packets[0])
            {
                return users;
            }

            // deserialize all users and add them to the list
            for (int i = 1; i < packets.Length; ++i)
            {
                string serializedUser;

                try
                {
                    serializedUser = Encoding.UTF8.GetString(Convert.FromBase64String(packets[i]));
                }
                catch (Exception)
                {
                    continue;
                }

                var newUser = SerializationHelper.DeserializeUser(serializedUser,_serialNumber);

                if (newUser != null)
                {
                    users.Add(newUser);
                }
            }

            return users;
        }

        /// <returns>Names of unregistered users.</returns>
        public List<String> GetUnregisteredUsers()
        {
            var users = new List<String>();

            String[] packets = _tcpClient.SendSynchronousRequest(RequestCode.USERS_UNREGISTERED);

            if (packets.Length < 2 || RequestCode.USERS_UNREGISTERED != packets[0])
            {
                return users;
            }

            // deserialize all users and add them to the list
            for (int i = 1; i < packets.Length; ++i)
            {
                users.Add(packets[i]);
            }

            return users;
        }



        /// <summary>Ask the remote device to provide the last inventory performed (if any).</summary>
        /// <returns>The last inventory.</returns>
        public InventoryData GetLastInventory()
        {
            string[] packets = _tcpClient.SendSynchronousRequest(RequestCode.LAST_INVENTORY);

            if (packets == null || packets.Length < 2 || RequestCode.LAST_INVENTORY != packets[0])
            {
                return null;
            }

            string serializedInventory;

            try
            {
                serializedInventory = Encoding.UTF8.GetString(Convert.FromBase64String(packets[1]));
            }
            catch (Exception)
            {
                return null;
            }

            return SerializationHelper.DeserializeInventory(serializedInventory);
        }

        /// <summary>Get a list of inventories over a given period.</summary>
        /// <param name='from'>Period start date.</param>
        /// <param name='to'>Period end date.</param>
        /// <returns>A list of InventoryData (empty if none, or if any error occurred).</returns>
        public List<InventoryData> GetInventories(DateTime from, DateTime to)
        {
            var result = new List<InventoryData>();

            // invalid date, or from >= to
            if (from >= to)
            {
                return result;
            }

            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0);
            var timespanStart = (long)(from.ToUniversalTime() - unixStart).TotalMilliseconds;
            var timespanEnd = (long)(to.ToUniversalTime() - unixStart).TotalMilliseconds;

            Console.WriteLine(timespanStart);
            Console.WriteLine(timespanEnd);

            String[] packets = _tcpClient.SendSynchronousRequest(10000, RequestCode.INVENTORIES_LIST,
                    timespanStart.ToString(), timespanEnd.ToString());

            if (packets.Length < 2 || RequestCode.INVENTORIES_LIST != packets[0])
            {
                return result;
            }

            for (int i = 1; i < packets.Length; ++i)
            {
                string serializedInventory;

                try
                {
                    serializedInventory = Encoding.UTF8.GetString(Convert.FromBase64String(packets[i]));
                }
                catch (Exception)
                {
                    return null;
                }

                InventoryData inv = SerializationHelper.DeserializeInventory(serializedInventory);
                inv.serialNumberDevice = _serialNumber;

                if (inv != null)
                {
                    result.Add(inv);
                }
            }

            return result;
        }

        /// <summary>Get The inventory by its id.</summary>
        /// <param name='id'>Id of the inventory desired.</param>
        /// <returns>A copy of InventoryData or null if something went wrong.</returns>
        public InventoryData GetInventoryById(int id)
        {
            if (id < 1 )
            {
                return null;
            }

            string[] packets = _tcpClient.SendSynchronousRequest(RequestCode.INVENTORY_BY_ID, id.ToString());

            if (packets == null || packets.Length < 2 || RequestCode.INVENTORY_BY_ID != packets[0])
            {
                return null;
            }

            string serializedInventory;

            try
            {
                serializedInventory = Encoding.UTF8.GetString(Convert.FromBase64String(packets[1]));
            }
            catch (Exception)
            {
                return null;
            }

            return SerializationHelper.DeserializeInventory(serializedInventory);
        }

        /// <summary>Get a set of temperature measure and measurement date for a given period.</summary>
        /// <param name='from'>Period start date.</param>
        /// <param name='to'>Period end date.</param>
        /// <returns>Set of all measures (value) and their date (key) (empty if no measure or error).</returns>
        public Dictionary<DateTime, Double> GetTemperatureMeasures(DateTime from, DateTime to)
        {
            var result = new Dictionary<DateTime, double>();

            // invalid date, or from >= to
            if (from >= to)
            {
                return result;
            }

            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0);
            var timespanStart = (long)(from.ToUniversalTime() - unixStart).TotalMilliseconds;
            var timespanEnd = (long)(to.ToUniversalTime() - unixStart).TotalMilliseconds;

            string[] packets = _tcpClient.SendSynchronousRequest(10000, RequestCode.TEMPERATURE_LIST,
                    timespanStart.ToString(), timespanEnd.ToString());

            if (
                // if number of packets < 3, it means that no values where returned for this period
                packets.Length < 3 ||
                // number of packets must be ODD (ResponseCode + X*2 packets where X is number of values)
                (packets.Length & 1) != 1 ||
                RequestCode.TEMPERATURE_LIST != packets[0])
            {
                return result;
            }

            for (int i = 1; i < packets.Length; i += 2)
            {
                try
                {
                    // timestamp in seconds and temperature measurement value
                    long timestamp = long.Parse(packets[i]);
                    double value = Double.Parse(packets[i + 1]);

                    var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                    dateTime = dateTime.AddSeconds(timestamp).ToLocalTime();
                    result[dateTime] = value;
                }
                catch (Exception e)
                {
                    if (e is FormatException || e is OverflowException)
                    {
                        // TODO: Log exception
                        Console.WriteLine("Invalid timestamp or temperature measure provided.");
                    }
                }
            }

            return result;
        }


        /// <returns>"Real time" status of the remote device or DeviceStatus.DS_InError if any error occurred.</returns>
		public DeviceStatus GetImmediateStatus()
		{
	        String[] packets = _tcpClient.SendSynchronousRequest(RequestCode.DEVICE_STATUS);
	
	        if(packets == null || packets.Length < 2 || RequestCode.DEVICE_STATUS != packets[0])
	        {
	            return DeviceStatus.DS_InError;
	        }
	
			return NewToOldEnumStatus(packets[1]);
		}

        /// <summary>Send a Scan request to the remote device.</summary>
        public void RequestScan(bool isContinous)
        {
            if (isContinous == true)
            {
                _tcpClient.Send(RequestCode.START_CONTINOUS_SCAN);
            }
            else
            {
                _tcpClient.Send(RequestCode.SCAN);
            }
        }

        public void RequestScan(ScanOption[] scanOption)
        {
            if (scanOption != null)
            {
                switch (scanOption.Length)
                {
                    case 0:
                        this._tcpClient.Send("scan", new string[0]);
                        break;
                    case 1:
                        this._tcpClient.Send("scan", new string[]
					{
						scanOption[0].ToString()
					});
                        break;
                    case 2:
                        this._tcpClient.Send("scan", new string[]
					{
						scanOption[0].ToString(),
						scanOption[1].ToString()
					});
                        break;
                    case 3:
                        this._tcpClient.Send("scan", new string[]
					{
						scanOption[0].ToString(),
						scanOption[1].ToString(),
						scanOption[2].ToString()
					});
                        break;
                    default:
                        this._tcpClient.Send("scan", new string[0]);
                        break;
                }
            }
        }

        /// <summary>Ask the device to stop the current scan (if any).</summary>
        public void StopScan()
        {
            _tcpClient.Send(RequestCode.STOP_SCAN);
        }

        /// <summary>Ask the device to stop the current scan (if any).</summary>
        public bool StopContinuosScan()
        {
            String[] packets = _tcpClient.SendSynchronousRequest(RequestCode.STOP_CONTINOUS_SCAN);

            if (packets.Length < 2 || RequestCode.STOP_CONTINOUS_SCAN != packets[0])
            {
                return false;
            }

            return "true" == packets[1];            
        }


        /// <summary>Send a Scan request to the remote device.</summary>
        public void SetLightList(List<string> lightList)
        {
            _tcpClient.Send(RequestCode.SET_LIGHT_LIST, lightList.ToArray());
            //return "true" == packets[1];
        }

        /// <summary>Send a Scan request to the remote device.</summary>
        /*public void RequestScan(ScanOtion[] so)
        {
            _tcpClient.Send(RequestCode.SCAN,);
        }*/



        /// <summary>Ask the remote device to start a led-lighting process.</summary>
        /// <param name='tagsUid'>Tags to be lighted.</param>
        /// <returns>True if the operation started successfully, false otherwise.</returns>
        public bool StartLightingTagsLed(List<string> tagsUid)
        {
            if (tagsUid == null || tagsUid.Count == 0)
            {
                return false;
            }

            String[] packets =
                    _tcpClient.SendSynchronousRequest(5000, RequestCode.START_LIGHTING, tagsUid.ToArray());

            if (packets.Length < 2 || RequestCode.START_LIGHTING != packets[0])
            {
                return false;
            }
            // let up to 5000ms to the device to kz/load&confirm/kb/kl all UID's
            int timeout = 2000 + tagsUid.Count*500;
            if (_lightingStartedEvent.WaitOne(timeout))
            {
                tagsUid.RemoveAll(uid => !_tagsNotLighted.Contains(uid));
                // return true only if smartserver sent true and all tags were lighted
                return "true" == packets[1] && _tagsNotLighted.Count == 0;
            }

            // the event was not sent...
            return false;
            //return "true" == packets[1];
        }

        /// <summary>Ask the device to stop the current led-lighting process (if any)</summary>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public bool StopLightingTagsLed()
        {
            String[] packets = _tcpClient.SendSynchronousRequest(RequestCode.STOP_LIGHTING);

            if (packets.Length < 2 || RequestCode.STOP_LIGHTING != packets[0])
            {
                return false;
            }

            return "true" == packets[1];
        }

        /// <summary>Ask the remote device to (re)write a tag UID.</summary>
        /// <param name='oldUid'>Current UID</param>
        /// <param name='newUid'>New UID</param>
        /// <returns>A value from WriteCode.</returns>
        public WriteCode RewriteUid(string oldUid, string newUid)
        {
            string[] packets =
                    _tcpClient.SendSynchronousRequest(6000, RequestCode.REWRITE_UID, oldUid, newUid);

            if (RequestCode.REWRITE_UID != packets[0] || packets.Length < 2)
            {
                return WriteCode.WC_Error;
            }


            switch (packets[1])
            {
                case "TAG_NOT_DETECTED":
                    return WriteCode.WC_TagNotDetected;

                case "TAG_NOT_CONFIRMED":
                    return WriteCode.WC_TagNotConfirmed;

                case "TAG_BLOCKED_OR_NOT_SUPPLIED":
                    return WriteCode.WC_TagBlockedOrNotSupplied;

                case "TAG_BLOCKED":
                    return WriteCode.WC_TagBlocked;

                case "TAG_NOT_SUPPLIED":
                    return WriteCode.WC_TagNotSupplied;

                case "WRITING_CONFIRMATION_FAILED":
                    return WriteCode.WC_ConfirmationFailed;

                case "WRITING_SUCCESS":
                    return WriteCode.WC_Success;

                case "NEW_UID_INVALID":
                case "ERROR":
                default:
                    return WriteCode.WC_Error;
            }
        }

        public String getLastBadgeScanned()
        {
            return _lastBadgeScanned;
        }
        /// <returns>Current temperature (°C) or TEMPERATURE_ERROR if an error occurred.</returns>
        public double GetCurrentTemperature()
        {
            try
            {
                String[] packets = _tcpClient.SendSynchronousRequest(RequestCode.TEMPERATURE_CURRENT);

                if (packets.Length != 2 || RequestCode.TEMPERATURE_CURRENT != packets[0])
                {
                    return TEMPERATURE_ERROR;
                }

                return Double.Parse(packets[1]);
            }
            catch (Exception e)
            {
                if (e is FormatException || e is OverflowException)
                {
                    // TODO: log exception
                    Console.WriteLine("Invalid temperature measure.");
                }
            }

            return TEMPERATURE_ERROR;
        }


        #endregion

        
        #region Event Handling
        /// <summary>
        /// Called by the TcpClient when an <see cref="Client.EventCode"/> is sent by the remote device.
        /// </summary>
        /// <param name='packets'>
        /// Packets sent by the remote device.
        /// </param>
        public void HandleEvent(string[] packets)
        {
            rfidReaderArgs args;

            switch (packets[0])
            {
                case EventCode.BADGE_SCANNED:
                    if (packets.Length < 2)
                    {
                        return;
                    }
                    _lastBadgeScanned = packets[1];
                    break;
                case EventCode.DEVICE_DISCONNECTED:
                    _deviceStatus = DeviceStatus.DS_NotReady;

                    args = new rfidReaderArgs(_serialNumber,
                                             rfidReaderArgs.ReaderNotify.RN_Disconnected,
                                             String.Empty);
                    RaiseEvent(args);
                    break;

                case EventCode.DOOR_OPENED:
                    _deviceStatus = DeviceStatus.DS_DoorOpen;

                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_Door_Opened,
                                          String.Empty);
                    RaiseEvent(args);
                    break;

                case EventCode.DOOR_CLOSED:
                    _deviceStatus = DeviceStatus.DS_DoorClose;

                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_Door_Closed,
                                          String.Empty);
                    RaiseEvent(args);
                    break;

                case EventCode.DOOR_OPEN_DELAY:
                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_DoorOpenTooLong,
                                          String.Empty);
                    RaiseEvent(args);
                    break;

                case EventCode.SCAN_STARTED:
                    _deviceStatus = DeviceStatus.DS_InScan;

                    CptTag = 0;
                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_ScanStarted,
                                          String.Empty);
                    RaiseEvent(args);
                    break;

                case EventCode.SCAN_CANCELLED_BY_HOST:
                    _deviceStatus = DeviceStatus.DS_Ready;

                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_ScanCancelByHost,
                                          String.Empty);
                    RaiseEvent(args);
                    break;

                case EventCode.SCAN_COMPLETED:
                    _deviceStatus = DeviceStatus.DS_Ready;
                    ScanId++;
                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_ScanCompleted,
                                          ScanId.ToString());
                    RaiseEvent(args);
                    break;

                case EventCode.SCAN_FAILED:
                    _deviceStatus = DeviceStatus.DS_Ready;

                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_ReaderFailToStartScan,
                                          String.Empty);
                    RaiseEvent(args);
                    break;

                case EventCode.TAG_ADDED:
                    if (packets.Length < 2)
                    {
                        return;
                    }

                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_TagAdded,
                                          packets[1]);
                    CptTag++;
                    RaiseEvent(args);
                    break;

                case EventCode.ENROLLMENT_SAMPLE:
                    // packets[1] contain the number of the sample
                    // example: 3 (3rd sample over the 4 required)
                    Console.WriteLine("Sample " + packets[1] + "/4 acquired for enrollement.");
                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_EnrollmentSample,packets[1]);
                    RaiseEvent(args);
                    break;

                case EventCode.ALERT:
                    // packets[1] contains the base64 encoded serialization of the alert raised
                    // Not used in C# SDK: drop it
                    break;

                case EventCode.AUTHENTICATION_SUCCESS:
                case EventCode.AUTHENTICATION_FAILURE:
                    // packets[1] contains base64 encoded serialization of the user
                    // packets[2] contains the AccessType enum value ("UNDEFINED", "BADGE", "FINGERPRINT")
                    // packets[3] contains the reader type ("master" or "slave")
                    break;

                case EventCode.FINGER_TOUCHED:
                    // packets[1] contains the reader type ("master" or "slave")
                    break;

                case EventCode.LIGHTING_STARTED:
                    _deviceStatus = DeviceStatus.DS_LedOn;
                    _tagsNotLighted = packets.Skip(1).ToList();
                    if (_lightingStartedEvent != null)
                        _lightingStartedEvent.Set();
                    break;

                case EventCode.LIGHTING_STOPPED:
                    Console.WriteLine("Event: Lighting stopped");
                    break;

                case EventCode.TAG_PRESENCE:
                case EventCode.SCAN_CANCELLED_BY_DOOR:
                    // No appropriate event in C# SDK. Not even sure it's used by the Firmware.
                    break;

                case EventCode.TEMPERATURE_MEASURE:
                    // Not used in C# SDK
                    // packets[1] contains a string convertible to double (example "25.4")
                    break;
                case EventCode.STATUS_CHANGED:
						if(packets.Length < 2)
						{
							return;
						}
						_deviceStatus = NewToOldEnumStatus(packets[1]);
						Console.WriteLine ("**Status Changed: "+_deviceStatus);
						break;
                case EventCode.CONNECTED:
                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_Client_Connected,packets[1]);
                    RaiseEvent(args);
                        break;
                case EventCode.LED_ON_FOUND:
                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_Led_Found,packets[1]);
                    RaiseEvent(args);
                    break;
                case EventCode.SET_MODIFIED_LIGHTLIST:
                    args = new rfidReaderArgs(_serialNumber, rfidReaderArgs.ReaderNotify.RN_SetLightListModified,packets[1]);
                    RaiseEvent(args);
                    break;

            }
        }

        /// <summary>Raise a DeviceEvent event when appropriate.</summary>
        /// <param name='args'>Data to be passed to the event listener.</param>
        private void RaiseEvent(rfidReaderArgs args)
        {
            ThreadPool.QueueUserWorkItem(
            delegate
            {
                var handler = DeviceEvent;

                if (handler != null)
                {
                    DeviceEvent(args);
                }
            }
            , null);

        }
        #endregion

        #region Getters
        /// <returns>Device serial number (initialized on connection).</returns>
        public string GetSerialNumber()
        {
            return _serialNumber;
        }

        /// <returns>Device hardware version (initialized on connection).</returns>
        public string GetHardwareVersion()
        {
            return _hardwareVersion;
        }

        /// <returns>Device software version (initialized on connection).</returns>
        public string GetSoftwareVersion()
        {
            return _softwareVersion;
        }

        /// <returns>DeviceStatus value.</returns>
        public DeviceStatus GetStatus()
        {
            return _deviceStatus;
        }

        /// <returns>DeviceType value (initialized on connection).</returns>
        public new DeviceType GetType()
        {
            return _deviceType;
        }
        #endregion
    }

    /// <summary>Contains all EventCode values sent by the device over the network.</summary>
    public static class EventCode
    {
        public const string ALERT = "event_alert";

        public const string AUTHENTICATION_SUCCESS = "event_authentication_success";
        public const string AUTHENTICATION_FAILURE = "event_authentication_failure";

        public const string BADGE_SCANNED = "event_badge_scanned";

        public const string DEVICE_DISCONNECTED = "event_device_disconnected";

        public const string DOOR_OPENED = "event_door_opened";
        public const string DOOR_CLOSED = "event_door_closed";
        public const string DOOR_OPEN_DELAY = "event_door_open_delay";

        public const string ENROLLMENT_SAMPLE = "event_enrollment_sample";
        public const string FINGER_TOUCHED = "event_finger_touched";

        public const string LIGHTING_STARTED = "event_lighting_started";
        public const string LIGHTING_STOPPED = "event_lighting_stopped";

        public const string SCAN_CANCELLED_BY_DOOR = "event_scan_cancelled_by_door";
        public const string SCAN_CANCELLED_BY_HOST = "event_scan_cancelled_by_host";
        public const string SCAN_COMPLETED = "event_scan_completed";
        public const string SCAN_FAILED = "event_scan_failed";
        public const string SCAN_STARTED = "event_scan_started";

        public const string STATUS_CHANGED = "event_status_changed";

        public const string TAG_ADDED = "event_tag_added";
        public const string TAG_PRESENCE = "event_tag_presence";
        public const string TEMPERATURE_MEASURE = "event_temperature_measure";
        public const string CONNECTED = "event_connected";
        public const string LED_ON_FOUND = "event_led_found";
        public const string SET_MODIFIED_LIGHTLIST = "event_setModifiedlightlist";
    }


    /// <summary>Contains all ResponseCode values used to communicate with the device.</summary>
    public static class RequestCode
    {
        // add an alert into TcpDevice db
        public const string ADD_ALERT = "addalert";
        // add an user into TcpDevice db & users service
        public const string ADD_USER = "adduser";
        // get list of alerts from TcpDevice db
        public const string ALERTS_LIST = "alertslist";
        // ask the TcpDevice to close the asynchronous TCP/IP socket with this client
        // Get the current "DeviceStatus" value of the remote device
		public const string DEVICE_STATUS	   = "devicestatus";

        public const string DISCONNECT = "disconnect";
        // start a fingerprint enrollment process
        public const string ENROLL_FINGER = "enrollfinger";
        // get basic information, required for TcpDevice initialization
        public const string INITIALIZATION = "initialization";
        // get a set of inventories over a period (start and end dates provided by user)
       // public const string INVENTORY_RANGE = "inventoryrange";
        public const string INVENTORIES_LIST = "inventorieslist";
        // Get Inventory by ID
        public const string INVENTORY_BY_ID = "inventorybyid";
        // get the last alert recorded by the TcpDevice
        public const string LAST_ALERT = "lastalert";
        // get the last inventory performed by the TcpDevice
        public const string LAST_INVENTORY = "lastinventory";
        // remove an alert from TcpDevice db
        public const string REMOVE_ALERT = "removealert";
        // remove a fingerprint from TcpDevice db and users service
        public const string REMOVE_FINGERPRINT = "removefingerprint";
        // remove a user from TcpDevice db and users service
        public const string REMOVE_USER = "removeuser";
        // peform a TAG Uid rewriting operation
        public const string REWRITE_UID = "rewriteuid";
        // ask for a scan to start
        public const string SCAN = "scan";
        // set a new SMTP server configuration in TcpDevice db
        public const string SET_SMTP_SERVER = "setsmtpserver";
        // set a "thief finger" value for a given user ("thief finger" alert)
        public const string SET_THIEF_FINGER = "setthieffinger";
        // ask the device to switch ON or OFF the "(USB to) serial bridge"
        public const string SERIAL_BRIDGE = "serialbridge";
        // get the SMTP server information from TcpDevice db
        public const string SMTP_SERVER = "smtpserver";
        // ask to start a LED lighting operation
        public const string START_LIGHTING = "startlighting";
        // ask to stop a LED lighting operation
        public const string STOP_LIGHTING = "stoplighting";
        // ask to stop current scan (if any)
        public const string STOP_SCAN = "stopscan";
        // get current temperature measurement from TcpDevice
        //public const string TEMP_CURRENT = "tempcurrent";
        public const string TEMPERATURE_CURRENT = "temperaturecurrent";
        // get a range of temperature measurements over a period (start and end dates provided by user)
        //public const string TEMP_RANGE = "temprange";
        public const string TEMPERATURE_LIST = "temperaturelist";
        // ask to update a given Alert
        public const string UPDATE_ALERT = "updatealert";
        // ask to update a Badge number in TcpDevice db and users service
        public const string UPDATE_BADGE = "updatebadge";
        // ask to update a permission in TcpDevice db and users service
        public const string UPDATE_PERMISSION = "updatepermission";
        // get a GrantedUser by name
        public const string USER_BY_NAME = "userbyname";
        // get GrantedUsers list.
        public const string USERS_LIST = "userslist";
        // get diabled user
        public const String USERS_UNREGISTERED = "usersunregistered";
        //start continous scan
        public const String START_CONTINOUS_SCAN = "startcontinousscan";
        //stop continous scan
        public const String STOP_CONTINOUS_SCAN = "stopcontinousscan";
        //set tags to light in continous scan
        public const String SET_LIGHT_LIST = "setlightlist";
    }

    /**
     * Response Codes available after a Rewrite Tag UID request.
     */
    public static class RewriteUIDResult
    {
        /** Unexpected/Unknown error */
        public const byte ERROR = 0;
        public const byte TAG_NOT_DETECTED = 1;
        public const byte TAG_NOT_CONFIRMED = 2;
        public const byte TAG_BLOCKED_OR_NOT_SUPPLIED = 3;
        public const byte TAG_BLOCKED = 4;
        public const byte TAG_NOT_SUPPLIED = 5;
        public const byte WRITING_CONFIRMATION_FAILED = 6;
        public const byte WRITING_SUCCESS = 7;
        /** New UID given by User doesn't match with UID alphanumeric rules (see TagUIDHandler) */
        public const byte NEW_UID_INVALID = 8;
    }

    /// <summary>
    /// Serialize and Deserialize methods for Inventories and Users.
    /// The Java SDK serializes data with XML format and SmartServer receives/sends base64 encoded data.
    /// SerializationHelper use System.Xml.XmlDocument to read and extract data from the given string.
    /// </summary>
    public static class SerializationHelper
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        /// <summary>Provide an InventoryData from an XML Inventory sent by SmartServer</summary>
        /// <param name='serializedInventory'>XML-serialized inventory.</param>
        /// <returns>InventoryData instance filled with the serialized inventory info., or null if error.</returns>
        public static InventoryData DeserializeInventory(string serializedInventory)
        {
            var inventory = new InventoryData();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(serializedInventory);

            var nodeId = xmlDoc.SelectSingleNode("/inventory/id");

            if (nodeId != null)
            {
                int inventoryId = 0;

                if (Int32.TryParse(nodeId.InnerText, out inventoryId))
                {
                    inventory.IdScanEvent = inventoryId;
                }
            }


            var nodeDate = xmlDoc.SelectSingleNode("/inventory/date");

            if (nodeDate != null)
            {
                try
                {
                    //inventory.eventDate = DateTime.ParseExact(nodeDate.InnerText,"MM/dd/yyyy HH:mm:ss",null); //12/02/2014 15:36:36
                    inventory.eventDate = UnixTimeStampToDateTime(double.Parse(nodeDate.InnerText));

                }
                catch (FormatException)
                {
                    Console.WriteLine("FormatException: Parsing Inventory date.");
                }
            }

            foreach (XmlNode tagNode in xmlDoc.SelectNodes("/inventory/presentTags/tag"))
            {
                inventory.listTagAll.Add(tagNode.InnerText);
                inventory.listTagPresent.Add(tagNode.InnerText);
            }

            foreach (XmlNode tagNode in xmlDoc.SelectNodes("/inventory/addedTags/tag"))
            {
                inventory.listTagAll.Add(tagNode.InnerText);
                inventory.listTagAdded.Add(tagNode.InnerText);
            }

            foreach (XmlNode tagNode in xmlDoc.SelectNodes("/inventory/removedTags/tag"))
            {
                inventory.listTagRemoved.Add(tagNode.InnerText);
            }

            var nodeUsername = xmlDoc.SelectSingleNode("/inventory/username");

            if (nodeUsername != null)
            {
                if (nodeUsername.InnerText.Contains('_'))
                {
                    string[] log = nodeUsername.InnerText.Split('_');
                    inventory.userFirstName = log[0];
                    inventory.userLastName = log[1];
                }
                else
                {
                    inventory.userFirstName = nodeUsername.InnerText;
                    inventory.userLastName = nodeUsername.InnerText;
                }
                
            }

            var nodeAccessType = xmlDoc.SelectSingleNode("/inventory/accessType");

            if (nodeAccessType != null)
            {
                switch (nodeAccessType.InnerText)
                {
                    case "FINGERPRINT":
                        inventory.accessType = AccessType.AT_FINGERPRINT;
                        break;

                    case "BADGE":
                        inventory.accessType = AccessType.AT_BADGEREADER;
                        break;
                }
            }
            XmlNode xmlNode6 = xmlDoc.SelectSingleNode("/inventory/doorNumber");
            if (xmlNode6 != null)
            {
                string innerText = xmlNode6.InnerText;
                if (innerText != null)
                {

                    if (innerText == "0")
                    {
                        inventory.userDoor = DoorInfo.DI_MASTER_DOOR;
                    }


                    else if (innerText == "1")
                    {
                        inventory.userDoor = DoorInfo.DI_SLAVE_DOOR;
                    }
                    else
                    {
                        inventory.userDoor = DoorInfo.DI_NO_DOOR;
                    }
                }
            }


            inventory.nbTagAdded = inventory.listTagAdded.Count;
            inventory.nbTagAll = inventory.listTagAll.Count;
            inventory.nbTagPresent = inventory.listTagPresent.Count;
            inventory.nbTagRemoved = inventory.listTagRemoved.Count;

            return inventory;
        }

        /// <summary>Provide a DeviceGrant from an XML GrantedUser sent by SmartServer</summary>
        /// <param name='serializedUser'>XML-serialized user.</param>
        /// <returns>DeviceGrant instance filled with the serialized user info., or null if error.</returns>
        public static DeviceGrant DeserializeUser(string serializedUser, string serialNumber)
        {
            var dg = new DeviceGrant();
            dg.serialRFID = serialNumber;
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(serializedUser);

            var nodeUsername = xmlDoc.SelectSingleNode("/user/username");

            if (nodeUsername != null)
            {
                string[] login = nodeUsername.InnerText.Split('_');
                if (login.Length > 1)
                {
                    dg.user.firstName = login[0];
                    dg.user.lastName = login[1];
                }
                else
                {
                    dg.user.firstName = nodeUsername.InnerText;
                    dg.user.lastName = nodeUsername.InnerText;
                }
            }

            var nodeBadgeNumber = xmlDoc.SelectSingleNode("/user/badgeNumber");

            if (nodeBadgeNumber != null)
            {
                dg.user.BadgeReaderID = nodeBadgeNumber.InnerText;
            }

            var nodeGrantType = xmlDoc.SelectSingleNode("/user/grantType");

            if (nodeGrantType != null)
            {
                switch (nodeGrantType.InnerText)
                {
                    case "ALL":
                        dg.userGrant = UserGrant.UG_MASTER_AND_SLAVE;
                        break;

                    case "MASTER":
                        dg.userGrant = UserGrant.UG_MASTER;
                        break;

                    case "SLAVE":
                        dg.userGrant = UserGrant.UG_SLAVE;
                        break;

                    default:
                        dg.userGrant = UserGrant.UG_NONE;
                        break;
                }
            }

            var fingerNodes = xmlDoc.SelectNodes("/user/fingers/finger");

            if (fingerNodes.Count > 0)
            {
                var userClass = new UserClass();
                userClass.firstName = dg.user.firstName;
                userClass.lastName = dg.user.lastName;
                userClass.EnrolledFingersMask = 0;
                foreach (XmlNode fingerNode in fingerNodes)
                {
                    var fingerIndex = GetDpfpFingerIndex(fingerNode.Attributes["index"].Value);
                    userClass.strFingerprint[(int)fingerIndex] = fingerNode.InnerText;
                    dg.user.isFingerEnrolled[(int)fingerIndex] = true;
                    int pow = int.Parse(fingerNode.Attributes["index"].Value);
                    userClass.EnrolledFingersMask += (int) Math.Pow(2, pow);
                }

                var bf = new BinaryFormatter();
                var ms = new MemoryStream();
                bf.Serialize(ms, userClass);
                dg.user.template = Convert.ToBase64String(ms.ToArray());
            }

            return dg;
        }

        /// <summary>Provide an XML-serialized user from the info. of a given DeviceGrant instance.</summary>
        /// <param name='serializedInventory'>User to be serialized.</param>
        /// <returns>String containing the XML-serialized user.</returns>
        public static string SerializeUser(DeviceGrant newUser)
        {

            string login = newUser.user.firstName;
            if (newUser.user.lastName != null)
                login = newUser.user.firstName + "_" + newUser.user.lastName;

            var sb = new StringBuilder();
            sb.Append("<user>");
            sb.Append("<username>").Append(login).Append("</username>");
            sb.Append("<badgeNumber>").Append(newUser.user.BadgeReaderID).Append("</badgeNumber>");

            sb.Append("<grantType>");
            switch (newUser.userGrant)
            {
                case UserGrant.UG_MASTER_AND_SLAVE:
                    sb.Append("ALL");
                    break;

                case UserGrant.UG_MASTER:
                    sb.Append("MASTER");
                    break;

                case UserGrant.UG_SLAVE:
                    sb.Append("SLAVE");
                    break;

                default:
                    sb.Append("UNDEFINED");
                    break;
            }

            sb.Append("</grantType>");
            sb.Append("<fingers>");

            if (newUser.user.template != null)
            {
                var bf = new BinaryFormatter();
                var ms = new MemoryStream(Convert.FromBase64String(newUser.user.template));
                var userClass = (UserClass)bf.Deserialize(ms);

                for (int i = 0; i < 10; ++i)
                {
                    if (userClass.strFingerprint[i] == null)
                    {
                        continue;
                    }

                    sb.Append("<finger index=\"");
                    sb.Append(i);
                    sb.Append("\">");
                    // TODO: Check that the given template is compatible with the format used by SmartServer (UareU 2.x)
                    sb.Append(userClass.strFingerprint[i]);
                    sb.Append("</finger>");
                }
            }

            sb.Append("</fingers>");
            sb.Append("</user>");

            return sb.ToString();
        }		

        /// <summary>
        /// Finger indexes used by SmartServer:
        /// 0: Left Pinky
        /// 1: Left Ring
        /// 2: Left Middle
        /// 3: Left Index
        /// 4: Left Thumb
        /// 5: Right Thumb
        /// 6: Right Index
        /// 7: Right Middle
        /// 8: Right Ring
        /// 9: Right Pinky
        /// </summary>
        /// <param name='fingerIndex'>C# SDK "FingerIndexValue" enum value</param>
        /// <returns>The corresponding value to be used with SmartServer (or 10 if invalid parameter given)</returns>
        public static int GetSmartServerFingerIndex(FingerIndexValue fingerIndex)
        {
            int oldIndex = (int)fingerIndex;

            if (oldIndex < 0 || oldIndex > 9)
            {
                return 10;
            }

            if (oldIndex < 5)
            {
                // right hand is 0->4 in C#, 5->9 in Java
                return oldIndex + 5;
            }

            // left hand is 5->9 (thumb to pinky) in C#, 0->4 in Java (pinky to thumb)
            return 9 - oldIndex;
        }

        /// <summary>
        /// Finger indexes used by DigitalPersona SDK:
        /// 0: Right Thumb
        /// 1: Right Index
        /// 2: Right Middle
        /// 3: Right Ring
        /// 4: Right Pinky
        /// 5: Left Thumb
        /// 6: Left Index
        /// 7: Left Middle
        /// 8: Left Ring
        /// 9: Left Pinky
        /// </summary>
        /// <param name='fingerIndex'>Value provided by SmartServer</param>
        /// <returns>The corresponding value to be used with DPFP SDK (or 10 if invalid parameter given)</returns>
        public static int GetDpfpFingerIndex(int oldIndex)
        {
            if (oldIndex < 0 || oldIndex > 9)
            {
                return 10;
            }

            if (oldIndex < 5)
            {
                return 9 - oldIndex;
            }

            return oldIndex - 5;
        }

        /// <summary>
        /// Overloaded method, handle string parameter (see int GetDpfpFingerIndex(int)).
        /// </summary>
        public static int GetDpfpFingerIndex(String oldIndex)
        {
            int fingerIndex;

            if (!Int32.TryParse(oldIndex, out fingerIndex))
            {
                return 10;
            }

            return GetDpfpFingerIndex(fingerIndex);
        }     
    }
}
