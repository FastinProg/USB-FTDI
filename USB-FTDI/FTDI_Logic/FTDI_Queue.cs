using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace USB_FTDI.FTDI_Logic
{
	/// <summary>
	/// Тип данных, готовыъ пакетов
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	unsafe public struct FTDI_Data_t
	{
		// Размер данных не более 64 байт
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		public byte[] data;
		public UInt32 ValidIndex; // Индекс с последним валидным значением
	};

	/// <summary>
	/// Очередь
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	unsafe public struct FTDI_Queue_t
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
		public byte[] dataRaw;          // Массив голых данных
		public uint lenghtRaw;          // Длина полученных голых данных
		public uint lastIndexRaw;		// Последний индекс проверенного байта

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 68 * 10)]
		public FTDI_Data_t[] dataPACK;        // Масив готовых пакетов

		public Byte HeadPack;         // Количество готовых для чтения пакетов
		public Byte TailPack;         // Количество прочитанных сообщений 	


		// Создать пакеты из голых данных
		public void CreatePack(Byte StartByte, Byte StopByte)
		{
			if (this.lenghtRaw == 0)
				return;

			// Cоздаем пакеты с данными, пока не дойдем до начала массива
			while (this.lenghtRaw != 0)
			{
				// Индекс стоп байта								
				int IndexStopByte = Array.IndexOf(this.dataRaw, StopByte , (int)this.lastIndexRaw, (int)this.lenghtRaw);

				// Если есть стоп байт
				if (IndexStopByte != -1)
				{
					// Проверяем длину
					if (IndexStopByte == 0)
						return;

					// Проверяем стартовый байт
					if (this.dataRaw[lastIndexRaw] == StartByte)
					{
						// Копируем данные из массива с голыми данными в массив с обработанныими
						Array.Copy(this.dataRaw, lastIndexRaw, this.dataPACK[HeadPack].data, 0, (IndexStopByte - lastIndexRaw));

						this.dataPACK[HeadPack].ValidIndex = ((byte)(IndexStopByte - lastIndexRaw));  // В массив с готовыми пакетами кладем длину
																									  // Имитация очереди
						if (++this.HeadPack >= 10)
							this.HeadPack = 0;

						lastIndexRaw = (uint)IndexStopByte;         // Смещаем индекс
					}
				}
				// В массиве нет готовых пакетов
                else
                {
					lastIndexRaw = 0;
					this.lenghtRaw = 0;
					Array.Clear(this.dataRaw, 0, 512);
				}
			}

		}
	};
}
