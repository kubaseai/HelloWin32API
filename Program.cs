using System;
using System.Drawing;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace WinBio {
	
	class WinBio {
	
		/*
		https://github.com/tpn/winsdk-10/blob/master/Include/10.0.16299.0/shared/winbio_types.h

		typedef struct _WINBIO_UNIT_SCHEMA {
			WINBIO_UNIT_ID UnitId;
			WINBIO_POOL_TYPE PoolType;
			WINBIO_BIOMETRIC_TYPE BiometricFactor;
			WINBIO_BIOMETRIC_SENSOR_SUBTYPE SensorSubType;
			WINBIO_CAPABILITIES Capabilities;
			WINBIO_STRING DeviceInstanceId;
			WINBIO_STRING Description;
			WINBIO_STRING Manufacturer;
			WINBIO_STRING Model;
			WINBIO_STRING SerialNumber;
			WINBIO_VERSION FirmwareVersion;
		} WINBIO_UNIT_SCHEMA, *PWINBIO_UNIT_SCHEMA;
		*/
				
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public class WINBIO_UNIT_SCHEMA
		{
			public UInt32 UnitId;
			public UInt32 PoolType;
			public UInt32 BiometricFactor;
			public UInt32 SensorSubType;
			public UInt32 Capabilities;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string DeviceInstanceId;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string Description;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string Manufacturer;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string Model;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string SerialNumber;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public ulong[] FirmwareVersion;			
		};
		
		[StructLayout(LayoutKind.Sequential)]
		public class GUID {
			public ulong Data1;
			public ushort Data2;
			public ushort Data3;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] Data4;
			public GUID() {
				this.Data1 = 0;
				this.Data2 = 0;
				this.Data3 = 0;
				this.Data4 = new byte[8];
				for (int i=0; i<8; i++) {
					this.Data4[i]=0;
				}
			}
			public GUID asWinbioDbDefault() {
				this.Data1=1;
				return this;
			}
		}

		//static uint WINBIO_ASYNC_NOTIFY_NONE = 0;
		static uint WINBIO_ASYNC_NOTIFY_CALLBACK = 1;
  		static uint WINBIO_ASYNC_NOTIFY_MESSAGE = 2;  		

		[DllImport("winbio.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)] 
		static extern int WinBioAsyncOpenSession(ulong type, ulong pool, ulong flags, uint[] ids, int idCount, IntPtr dbType, 
			uint NotificationMethod,
  			IntPtr  TargetWindow,
  			ushort MessageCode,
  			WinBioAsyncCompletionCallbackType CallbackCompletionRoutine,
 			IntPtr UserData,
			bool AsynchronousOpen,
			out IntPtr session);

		[DllImport("winbio.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)] 
		static extern int WinBioOpenSession(ulong type, ulong pool, ulong flags, uint[] ids, int idCount, IntPtr dbType, 
			out IntPtr session);

		[DllImport("winbio.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)] 
		static extern int WinBioEnumBiometricUnits(ulong type, out IntPtr schemaArr, out long count);
		
		public enum WinBioDatabaseId {
			Default = 1,
			Bootstrap = 2,
			OnChip = 3
		}
		
		const ulong WINBIO_TYPE_FACIAL_FEATURES = 0x00000002;
		const ulong WINBIO_TYPE_IRIS = 0x00000010;
		const ulong WINBIO_TYPE_THERMAL_FACE_IMAGE = 0x00000400;
		const ulong WINBIO_POOL_SYSTEM = 1;
		const ulong WINBIO_FLAG_RAW = 1;
		static IntPtr WINBIO_DB_DEFAULT = (IntPtr)WinBioDatabaseId.Default;
				
		const byte WINBIO_NO_PURPOSE_AVAILABLE = 0;
		const byte WINBIO_PURPOSE_VERIFY = 1;
		const byte WINBIO_PURPOSE_IDENTIFY = 2;
		const byte WINBIO_PURPOSE_ENROLL = 4;
		const byte WINBIO_PURPOSE_ENROLL_FOR_VERIFICATION = 8;
		const byte WINBIO_PURPOSE_ENROLL_FOR_IDENTIFICATION = 16;
		const byte WINBIO_PURPOSE_AUDIT = 0x80;

		const byte WINBIO_DATA_FLAG_RAW = 0x20;
		const byte WINBIO_DATA_FLAG_INTERMEDIATE = 0x40;
		const byte WINBIO_DATA_FLAG_PROCESSED = 0x80;
	
		public static WINBIO_UNIT_SCHEMA printUnitType(ulong type, String sType) {
			long unitCount = 1;
			IntPtr unitSchemaPtr = new IntPtr(0);
			int res = WinBioEnumBiometricUnits(type, out unitSchemaPtr, out unitCount);
			System.Console.WriteLine("WinBio unit of type "+sType+" count is "+unitCount);
			var size = Marshal.SizeOf(typeof(WINBIO_UNIT_SCHEMA));	
			WINBIO_UNIT_SCHEMA _unit = null;	
			for (int i=0; i < unitCount; i++) {
				WINBIO_UNIT_SCHEMA unit = (WINBIO_UNIT_SCHEMA)Marshal.PtrToStructure(unitSchemaPtr, typeof(WINBIO_UNIT_SCHEMA));
				String e = "["+(i+1)+"]";
				System.Console.WriteLine(e+" UnitId="+unit.UnitId);
				System.Console.WriteLine(e+" PoolType="+unit.PoolType);
				System.Console.WriteLine(e+" DeviceInstanceId="+ unit.DeviceInstanceId);
				System.Console.WriteLine(e+" Description="+ unit.Description);
				System.Console.WriteLine(e+" Manufacturer="+ unit.Manufacturer);
				System.Console.WriteLine(e+" Model="+ unit.Model);
				System.Console.WriteLine(e+" SerialNumber="+ unit.SerialNumber);
				_unit = unit;
				unitSchemaPtr = IntPtr.Add(unitSchemaPtr, size);				
			}
			System.Console.WriteLine(" ");
			return _unit;
		}
		
		[DllImport("winbio.dll", EntryPoint = "WinBioMonitorPresence")]
		private extern static int WinBioMonitorPresence(IntPtr session, ulong unitId);

		public delegate void WinBioAsyncCompletionCallbackType(IntPtr AsyncResult);

		public static void WinBioAsyncCompletionCallback(IntPtr AsyncResult) {
			System.Console.WriteLine("WinBioAsyncCompletionCallback");
		}

		public delegate void CaptureCallbackType(IntPtr CaptureCallbackContext, ulong OperationStatus, ulong UnitId,
		 IntPtr sample, long SampleSize, ulong RejectDetail);

		public static void CaptureCallback(IntPtr CaptureCallbackContext, ulong OperationStatus, ulong UnitId,
		 IntPtr sample, long SampleSize, ulong RejectDetail) {
			System.Console.WriteLine("Sample size="+SampleSize);
		}

		[DllImport("winbio.dll")]
        private extern static int WinBioCaptureSampleWithCallback(IntPtr session,
            byte purpose,
            byte flags,
            CaptureCallbackType cb,
            IntPtr context);

		
		[DllImport("winbio.dll")]
        private extern static int WinBioCaptureSample(IntPtr session,
            byte purpose,
            byte flags,
            out IntPtr unitId,
			out IntPtr sample,
			out IntPtr sampleSize,
            out IntPtr rejectDetails);

		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("winbio.dll")]
        private extern static int WinBioWait(IntPtr session);

		[DllImport("winbio.dll")]
        private extern static int WinBioCancel(uint sessionHandle);

		[DllImport("winbio.dll")]
        private extern static int WinBioCloseSession(IntPtr session);

		public static UInt32 getFaceIdUnitId() {
			long unitCount = 1;
			IntPtr unitSchemaPtr = new IntPtr(0);
			int res = WinBioEnumBiometricUnits(WINBIO_TYPE_FACIAL_FEATURES, out unitSchemaPtr, out unitCount);
			WINBIO_UNIT_SCHEMA unit = (WINBIO_UNIT_SCHEMA)Marshal.PtrToStructure(unitSchemaPtr, typeof(WINBIO_UNIT_SCHEMA));
			return unit.UnitId;
		}

		private delegate IntPtr WndProcType(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern System.UInt16 RegisterClassEx(
            [System.Runtime.InteropServices.In] ref WNDCLASSEX lpWndClass
        );
        
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct WNDCLASSEX {
			[MarshalAs(UnmanagedType.U4)]
			public int cbSize;
			[MarshalAs(UnmanagedType.U4)]
			public int style;
			public IntPtr lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpszMenuName;
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpszClassName;
			public IntPtr hIconSm;
		}

		[DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx")]
		public static extern IntPtr CreateWindowEx(
			int dwExStyle,
			//UInt16 regResult,
			[MarshalAs(UnmanagedType.LPStr)]
			string lpClassName,
			[MarshalAs(UnmanagedType.LPStr)]
			string lpWindowName,
			UInt32 dwStyle,
			int x,
			int y,
			int nWidth,
			int nHeight,
			IntPtr hWndParent,
			IntPtr hMenu,
			IntPtr hInstance,
			IntPtr lpParam);

        [DllImport("user32.dll")]
        static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		static ushort WINDOWS_HELLO_CB_MAGIC = 0xB123;

		private volatile static int helloCallbackCompleted = 0;
		private static UInt32 expectedFaceIdUnitId = 0;
		private static uint sessionHandle = 0;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct WINBIO_ASYNC_RESULT_UNION {
  			public UInt32 SessionHandle;
            public UInt32 Operation;
            public UInt64 SequenceNumber;
            public Int64 TimeStamp;
            public Int32 ApiStatus;
            public UInt32 UnitId;
            public IntPtr UserData;
			/// 
			public WINBIO_ASYNC_RESULT_PARAMETERS parameters;
		}

		 [StructLayout(LayoutKind.Sequential)]
        public struct WINBIO_ASYNC_RESULT_CAPTURESAMPLE
        {
			public IntPtr Sample;
            public ulong SampleSize;
            public UInt32 RejectDetail;
        }		
		
		[StructLayout(LayoutKind.Explicit)]
		public struct WINBIO_ASYNC_RESULT_PARAMETERS {
            
            [FieldOffset(0)]
            public WINBIO_ASYNC_RESULT_CAPTURESAMPLE CaptureSample;
			[FieldOffset(0)]
            public WINBIO_MONITOR_PRESENCE MonitorPresence;
        }
    
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct WINBIO_BIR_DATA {
			public UInt32 Size;
            public UInt32 Offset;
		}

		[StructLayout(LayoutKind.Sequential)]
        public struct WINBIO_BIR {
            public WINBIO_BIR_DATA HeaderBlock;
            public WINBIO_BIR_DATA StandardDataBlock;
            public WINBIO_BIR_DATA VendorDataBlock;
            public WINBIO_BIR_DATA SignatureBlock;
        }

		[StructLayout(LayoutKind.Sequential)]
        public struct WINBIO_BDB_ANSI_381_HEADER {
            public UInt64 RecordLength;
            public UInt32 FormatIdentifier;
            public UInt32 VersionNumber;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public UInt16[] ProductId;
            public UInt16 CaptureDeviceId;
            public UInt16 ImageAcquisitionLevel;
            public UInt16 HorizontalScanResolution;
            public UInt16 VerticalScanResolution;
            public UInt16 HorizontalImageResolution;
            public UInt16 VerticalImageResolution;
            public byte ElementCount;
            public byte ScaleUnits;
            public byte PixelDepth;
            public byte ImageCompressionAlg;
            public byte Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINBIO_BDB_ANSI_381_RECORD {
            public UInt32 BlockLength;
            public UInt16 HorizontalLineLength;
            public UInt16 VerticalLineLength;
            public byte Position;
            public byte CountOfViews;
            public byte ViewNumber;
            public byte ImageQuality;
            public byte ImpressionType;
            public byte Reserved;
        }

		[StructLayout(LayoutKind.Sequential)]
        public struct WINBIO_MONITOR_PRESENCE {
			public ulong ChangeType;
			public ulong PresenceCount;
		}
        
		[StructLayout(LayoutKind.Sequential)]
        public struct WINBIO_PRESENCE {
  			public UInt32 Factor;
  			public UInt32 SubFactor;
  			public UInt32 Status;
  			public UInt32 RejectDetail;/*
			WINBIO_IDENTITY            Identity;
			ULONGLONG                  TrackingId;
			WINBIO_PROTECTION_TICKET   Ticket;
			WINBIO_PRESENCE_PROPERTIES Properties;*/
		}

		public enum WINBIO_PRESENCE_CHANGE_TYPE {
			WINBIO_PRESENCE_CHANGE_TYPE_UNKNOWN = 0,
			WINBIO_PRESENCE_CHANGE_TYPE_UPDATE_ALL,
			WINBIO_PRESENCE_CHANGE_TYPE_ARRIVE,
			WINBIO_PRESENCE_CHANGE_TYPE_RECOGNIZE,
			WINBIO_PRESENCE_CHANGE_TYPE_DEPART,
			WINBIO_PRESENCE_CHANGE_TYPE_TRACK
		}
		private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam) {
			if (msg == WINDOWS_HELLO_CB_MAGIC) {
				WINBIO_ASYNC_RESULT_UNION ar = Marshal.PtrToStructure<WINBIO_ASYNC_RESULT_UNION>(lParam);
				String apiRes = new System.ComponentModel.Win32Exception(ar.ApiStatus).Message;
				String opName = ar.Operation == 12 ? "CAPTURE_SAMPLE" : (ar.Operation == 29 ? "MONITOR_PRESENCE" : "OTHER");
				String operation = opName+":"+ar.Operation;
				System.Console.WriteLine("WinBio cb seq="+ar.SequenceNumber+" received for UnitId="+ar.UnitId+", op="+operation+", api result="+apiRes);
				if (ar.UnitId == expectedFaceIdUnitId) {
					if (ar.Operation == 29) {
						//
						// MessageId: WINBIO_I_EXTENDED_STATUS_INFORMATION
						//
						// MessageText:
						//
						// Return data includes multiple status values, which must be checked separately.
						//
						// #define WINBIO_I_EXTENDED_STATUS_INFORMATION ((HRESULT)0x00090002L)
						// see: https://github.com/tpn/winsdk-10/blob/master/Include/10.0.16299.0/shared/winbio_err.h
						ulong change = ar.parameters.MonitorPresence.ChangeType;
						ulong count = ar.parameters.MonitorPresence.PresenceCount;
						IntPtr tableAddr = IntPtr.Add(lParam, 80);
						WINBIO_PRESENCE_CHANGE_TYPE changeType = (WINBIO_PRESENCE_CHANGE_TYPE)change;
						System.Console.WriteLine("- MonitorPresence change="+changeType+", count="+count);
						if (count > 0) {
							WINBIO_PRESENCE presence = Marshal.PtrToStructure<WINBIO_PRESENCE>(tableAddr);
							System.Console.WriteLine("-- MonitorPresence status="+presence.Status+", rejectDetail="+presence.RejectDetail);
							bool letsFinish = changeType == WINBIO_PRESENCE_CHANGE_TYPE.WINBIO_PRESENCE_CHANGE_TYPE_RECOGNIZE;
							if (presence.RejectDetail==0 && letsFinish) {
								WinBioCancel(ar.SessionHandle);
								sessionHandle = ar.SessionHandle;
								helloCallbackCompleted = 1;
							}
						}
					}
					//var cap = ar.parameters.CaptureSample;
					//System.Console.WriteLine("WinBio cb seq="+ar.SequenceNumber+", ts="+ar.TimeStamp+", sample size="+cap.SampleSize+", sample addr="+cap.Sample);
					/*if (cap.Sample.ToInt64() > 1 && cap.RejectDetail == 0) {
						WINBIO_BIR bir = Marshal.PtrToStructure<WINBIO_BIR>(cap.Sample);
						var ansi381HeaderAddr = IntPtr.Add(cap.Sample, (int)bir.StandardDataBlock.Offset);
                		var ansi381Header = Marshal.PtrToStructure<WINBIO_BDB_ANSI_381_HEADER>(ansi381HeaderAddr);
                		var ansi381HeaderLen = Marshal.SizeOf<WINBIO_BDB_ANSI_381_HEADER>();
                		var ansi381RecordAddr =  IntPtr.Add(ansi381HeaderAddr, ansi381HeaderLen);
						var ansi381Record = Marshal.PtrToStructure<WINBIO_BDB_ANSI_381_RECORD>(ansi381RecordAddr);
                		var ansi381RecordLen =  Marshal.SizeOf<WINBIO_BDB_ANSI_381_RECORD>();
                		var firstPixelAddr =  IntPtr.Add(ansi381RecordAddr, ansi381RecordLen);
                		var image = new byte[ansi381Record.BlockLength - ansi381RecordLen];
                		Marshal.Copy(firstPixelAddr, image, 0, image.Length);
						String info = String.Format("Image params: "+
						 "HorizontalLineLength={0}, "+
						 "VerticalLineLength={1}, "+
						 "HorizontalScanResolution={2}, "+
						 "VerticalScanResolution={3}, "+
						 "HorizontalImageResolution={4}, "+
						 "VerticalImageResolution={5}",
						 ansi381Record.HorizontalLineLength,
						 ansi381Record.VerticalLineLength,
                         ansi381Header.HorizontalScanResolution,
                         ansi381Header.VerticalScanResolution,
                         ansi381Header.HorizontalImageResolution,
                         ansi381Header.VerticalImageResolution);
						System.Console.WriteLine(info);						
					}*/
				}				
			}
			return DefWindowProc(hwnd, msg, wParam, lParam);
		}

		[DllImport("user32.dll")]
        static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        static extern bool TranslateMessage(MSG lpMsg);

        [DllImport("user32.dll")]
        static extern IntPtr DispatchMessage(MSG lpmsg);

		[StructLayout(LayoutKind.Sequential, Pack = 8)]  
		public struct MSG  
		{  
			public IntPtr hwnd;  
			public UInt32 message;  
			public UIntPtr wParam;  
			public UIntPtr lParam;  
			public UInt32 time;  
			public POINT pt;  
		}  
		public struct POINT
		{  
			public Int32 x;  
			public Int32 Y;  
		}

		public static void Main (string[] args) {
		 
			IntPtr session = new IntPtr(0);
			IntPtr newWndProc = Marshal.GetFunctionPointerForDelegate(new WndProcType(WndProc));

			WNDCLASSEX wind_class = new WNDCLASSEX();
            wind_class.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
            wind_class.style = 0x00020000;
            wind_class.hbrBackground = (IntPtr) 5;
            wind_class.cbClsExtra = 0;
            wind_class.cbWndExtra = 0;
            wind_class.hInstance = Process.GetCurrentProcess().Handle;
            wind_class.hIcon = IntPtr.Zero;
            wind_class.hCursor = IntPtr.Zero;
            wind_class.lpszMenuName = string.Empty;
            wind_class.lpszClassName = "WinBioCb";
            wind_class.lpfnWndProc = newWndProc;
            ushort atomWndClass = RegisterClassEx(ref wind_class);

			String err = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
			System.Console.WriteLine("RegisterClass err="+err);
    	    IntPtr msgProcessor = CreateWindowEx(1, "WinBioCb", "WinBioCb", 0x10000000|0x80000000, 0,0,100,3,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
			err = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
			System.Console.WriteLine("wind_class.hInstance="+wind_class.hInstance+", new window="+msgProcessor+", err="+err);

			printUnitType(WINBIO_TYPE_FACIAL_FEATURES, "WINBIO_TYPE_FACIAL_FEATURES");
			printUnitType(WINBIO_TYPE_IRIS, "WINBIO_TYPE_IRIS");
			printUnitType(WINBIO_TYPE_THERMAL_FACE_IMAGE, "WINBIO_TYPE_THERMAL_FACE_IMAGE");			

			expectedFaceIdUnitId = getFaceIdUnitId();
			MSG msg = new MSG();
			bool skipWarmUp = !true;
			if (!skipWarmUp) {
				int _result = WinBioAsyncOpenSession(
					WINBIO_TYPE_FACIAL_FEATURES,
					WINBIO_POOL_SYSTEM,
					WINBIO_FLAG_RAW, // Access: Capture raw data
					null,     		 // Array of biometric unit IDs
					0,               // Count of biometric unit IDs
					WINBIO_DB_DEFAULT,
					WINBIO_ASYNC_NOTIFY_MESSAGE,
					msgProcessor,
					WINDOWS_HELLO_CB_MAGIC,
					null,
					IntPtr.Zero,
					false,				
					out session
				);
				String _resultMsg = new System.ComponentModel.Win32Exception(_result).Message;
				System.Console.WriteLine("WinBio session result is "+_resultMsg+"\n"); // Access is denied when no Admin or SYSTEM
				
				int res = WinBioMonitorPresence(session, expectedFaceIdUnitId);
				String resMsg = new System.ComponentModel.Win32Exception(res).Message;
				System.Console.WriteLine("Sensor activation="+resMsg);

				while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0) {
					TranslateMessage(msg);
					DispatchMessage(msg);
					if (helloCallbackCompleted==1)
						break;
				}
				System.Console.WriteLine("Msg loop finished once.\n");
				WinBioCloseSession(session);
			}

			helloCallbackCompleted = 0;
			session = new IntPtr(0);
			int result = WinBioOpenSession(
				WINBIO_TYPE_FACIAL_FEATURES,
				WINBIO_POOL_SYSTEM,
				WINBIO_FLAG_RAW, // Access: Capture raw data
				null,		     // Array of biometric unit IDs
				0,               // Count of biometric unit IDs
				WINBIO_DB_DEFAULT,
				out session
			);
			String resultMsg = new System.ComponentModel.Win32Exception(result).Message;
			System.Console.WriteLine("WinBio session result is "+resultMsg+"\n"); // Access is denied when no Admin or SYSTEM
			IntPtr unitIds = new IntPtr(0);
			IntPtr sample = new IntPtr(0);
			IntPtr sampleSize = new IntPtr(0);
			IntPtr rejectDetails = new IntPtr(0);
			int status = WinBioCaptureSample(session, WINBIO_NO_PURPOSE_AVAILABLE, WINBIO_DATA_FLAG_RAW,
			 out unitIds, out sample, out sampleSize, out rejectDetails);
			String statusMsg = new System.ComponentModel.Win32Exception(status).Message;
			System.Console.WriteLine("Capturing requested, status="+statusMsg);
			//see: https://github.com/luspock/FingerPrint/blob/master/HeadFiles/winbio_err.h
			// https://github.com/takuya-takeuchi/WinBiometricDotNet/blob/master/sources/WinBiometricDotNet/Interop/SafeNativeMethods.cs
			// https://github.com/tpn/winsdk-10/blob/master/Include/10.0.16299.0/shared/winbio_err.h
		}
	}
}