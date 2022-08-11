using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FTD2XX_NET;
using USB_FTDI.FTDI_Logic;

namespace FTDI
{

	public class CAN_Message
	{
		public CAN_Message()
		{
			id = 0;
			data = new byte[8];
			dlc = 8;
			Ext = false;
		}

		public CAN_Message(UInt32 id, byte[] data, int dlc, int flags, long time)
		{
			this.id = id;
			this.data = data;
			this.dlc = dlc;
			this.flags = flags;
			this.time = time;
		}


		#region Свойства

		public UInt32 id { get; set; }
		public bool Ext { get; set; }
		public byte[] data { get; set; }
		public int dlc { get; set; }
		public int flags { get; set; }
		public long time { get; set; }

		public bool Handled { get; set; }

		#endregion


		public override string ToString()
		{
			//string pre = "0x";
			string pre = "";
			bool decodeUDS = true;

			string s;
			s = pre + id.ToString("X") + " " + (Ext ? "X " : "  ");
			for (int i = 0; i < dlc; i++)
			{
				s += pre + data[i].ToString("X2") + " ";
			}
			return s;
		}

	}

	class FTDI_Hardware
	{
		#region Поля класса

		public event Action<string> OnReadMessage;                                                                      // Событие приема пакета
		public Action<string> dCANDataRead;

		// Потоки для отправки и приема сообщений
		Thread txThread;
		Thread rxThread;
		// Флаги работы потока
		bool rxThreadAlive = true;
		bool txThreadAlive = true;
		// Локеры для потоков
		object txlocker = new object();
		object rxlocker = new object();
		// Задержка
		EventWaitHandle txWait = new AutoResetEvent(false);
		EventWaitHandle rxWait = new AutoResetEvent(false);
		// Поток для проверки потока
		Thread checkThread;
		bool isCheckThreadAlive = true;

		public FTDI_Queue_t RX_FTDI_Queue;


		private UInt32 ftdiDeviceCount;                                                                         // Номер устройства
		private FTD2XX_NET.FTDI myFtdiDevice;																	// Экземпляр класса, описывающий устройство
		private static FTD2XX_NET.FTDI.FT_STATUS ftStatus = FTD2XX_NET.FTDI.FT_STATUS.FT_OK;                    // Текущий статус

		#endregion

		public enum FTDI_Hardware_Status_e 
		{ 
			ftdiSt_OK,
			ftdiSt_DeviceNumberError,
			ftdiSt_DeviceInfoError,
			ftdiSt_DeviceOpenError,
			ftdiSt_DeviceSpeedSetingError,
			ftdiSt_DeviceSettingError,
			ftdiSt_DeviceFlowControlError,
			ftdiSt_DeviceReadWriteTimeout,
		};



		// Конструкутор
        public FTDI_Hardware()
        {
			RX_FTDI_Queue = new FTDI_Queue_t();

			RX_FTDI_Queue.HeadPack = 0;
			RX_FTDI_Queue.lastIndexRaw = 0;
			RX_FTDI_Queue.TailPack = 0;
			RX_FTDI_Queue.lenghtRaw = 0;
			RX_FTDI_Queue.dataRaw = new byte[512];
			RX_FTDI_Queue.dataPACK = new FTDI_Data_t[10];


			myFtdiDevice = new FTD2XX_NET.FTDI();
			ftdiDeviceCount = 0;
			dCANDataRead += FOO;
			OnReadMessage += dCANDataRead;
		}

		public void FOO(string s)
		{
			s += "ghbdtn";
		}
		// Установка соединения
		public FTDI_Hardware_Status_e Connect()
		{
			// Определяем количество подключенных устройств FTDI
			ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);

			// Проверка статуса
			if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
				return FTDI_Hardware_Status_e.ftdiSt_DeviceNumberError;

			// Проверка кол-ва устройств
			if (ftdiDeviceCount == 0)
				return FTDI_Hardware_Status_e.ftdiSt_DeviceNumberError;

			// Создаем массив с информацией об устройстве
			FTD2XX_NET.FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTD2XX_NET.FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

			// Заполняем список устройств
			ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

			// Проверка после заполнения информации об устройстве
			if (ftStatus == FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
			{
				// Можно вывести эту инфу
				/*
				for (UInt32 i = 0; i < ftdiDeviceCount; i++)
				{
					Console.WriteLine("Device Index: " + i.ToString());
					Console.WriteLine("Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags));
					Console.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
					Console.WriteLine("ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID));
					Console.WriteLine("Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId));
					Console.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
					Console.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
					Console.WriteLine("");
				}
				*/
			}
			else
				return FTDI_Hardware_Status_e.ftdiSt_DeviceInfoError;

			// Открываем COM Port
			ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
			
			// Проверка
			if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
				return FTDI_Hardware_Status_e.ftdiSt_DeviceOpenError;

			// Устанавливаем скорость 
			ftStatus = myFtdiDevice.SetBaudRate(9600);
			ftStatus = myFtdiDevice.SetBaudRate(3000000);
			ftStatus = myFtdiDevice.SetEventNotification(FTD2XX_NET.FTDI.FT_EVENTS.FT_EVENT_RXCHAR, rxWait);
			ftStatus = myFtdiDevice.SetLatency(1);
			if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
				return FTDI_Hardware_Status_e.ftdiSt_DeviceSpeedSetingError;
			
			// Установка параметров
			ftStatus = myFtdiDevice.SetDataCharacteristics(FTD2XX_NET.FTDI.FT_DATA_BITS.FT_BITS_8, FTD2XX_NET.FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTD2XX_NET.FTDI.FT_PARITY.FT_PARITY_NONE);
			if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
				return FTDI_Hardware_Status_e.ftdiSt_DeviceSettingError;

			// Set flow control - set RTS/CTS flow control 
			ftStatus = myFtdiDevice.SetFlowControl(FTD2XX_NET.FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
			if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
				return FTDI_Hardware_Status_e.ftdiSt_DeviceFlowControlError;


			// Set read timeout to 5 seconds, write timeout to infinite
			ftStatus = myFtdiDevice.SetTimeouts(1000, 1000);
			if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
				return FTDI_Hardware_Status_e.ftdiSt_DeviceReadWriteTimeout;

			rxThread = new Thread(recieve_thread);

			rxThread.Start();
			// Успешная установка соединения
			return FTDI_Hardware_Status_e.ftdiSt_OK;
		}

        public void recieve_thread()
        {
            uint numBytesAvailable = 0;             // Доступное кол-во байт для чтения
            uint numBytesRead = 0;                  // Кол-во прочитанных байт
            while (rxThreadAlive)
            {
                // Флаг события
                rxWait.WaitOne(100);

                // Получить кол-во доступных для чтения байт
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesAvailable);

				// Если есть доступные данные
                if (numBytesAvailable > 1)
                {
                    // Считываем данные из FTDI в програманый буфер
                    myFtdiDevice.Read(RX_FTDI_Queue.dataRaw, numBytesAvailable, ref RX_FTDI_Queue.lenghtRaw);
					RX_FTDI_Queue.CreatePack(0x78,0x23);

				}


            }
        }

    }

}

