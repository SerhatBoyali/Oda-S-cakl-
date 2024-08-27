using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Modbus.Device;
using System.IO.Ports;
using System.IO;


namespace OcaSıcaklığı
{
    public partial class Form1 : Form
    {
        private IModbusSerialMaster master;
        private SerialPort port;

        public Form1()
        {
            InitializeComponent();
            LoadAvailablePorts();
        }

        private void LoadAvailablePorts()
        {
            comboBoxPorts.Items.Clear();
            comboBoxPorts.Items.AddRange(SerialPort.GetPortNames());
        }

        private void comboBoxPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedPort = comboBoxPorts.SelectedItem.ToString();
            InitializeModbus(selectedPort, 9600);
        }

        private void comboBoxBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedBaudRate = int.Parse(comboBoxBaudRate.SelectedItem.ToString());
            string selectedPort = comboBoxPorts.SelectedItem.ToString();
            InitializeModbus(selectedPort, selectedBaudRate);
        }

        private void buttonReadData_Click(object sender, EventArgs e)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    labelDurum.Text = "AÇIK";
                    // İlk veri okumasını başlat
                    ReadTemperatureData();
                    // Zamanlayıcıyı başlat
                    timerReadData.Start();
                    timerLogData.Start();
                }
                else
                {
                    labelDurum.Text = "KAPALI";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Seri port açılırken hata oluştu: {ex.Message}");
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    labelDurum.Text = "KAPALI";
                    timerReadData.Stop();
                    timerLogData.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Seri port kapatılırken hata oluştu: {ex.Message}");
            }

        }

        private void InitializeModbus(string portName, int baudRate)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    port.Close();
                }

                port = new SerialPort(portName)
                {
                    BaudRate = baudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                };

                port.Open();
                master = ModbusSerialMaster.CreateRtu(port);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Seri port açılırken hata oluştu: {ex.Message}");
            }
        }

        private void ReadTemperatureData()
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    byte slaveId = 9;
                    ushort startAddress = 4; // 5. register (0-indexed olarak 4)
                    ushort numRegisters = 2; // 2 adet register okunacak

                    // Modbus RTU üzerinden register okuma
                    ushort[] registers = master.ReadHoldingRegisters(slaveId, startAddress, numRegisters);

                    // Register'ları float değere dönüştür
                    float temperature = ConvertRegistersToFloat(registers[1], registers[0]); // registers[1] yüksek byte, registers[0] düşük byte

                    // Ekrana yazdır
                    textBoxTemperature.Text = $"Oda Sıcaklığı: {temperature:F2} °C";
                }
                else
                {
                    MessageBox.Show("Seri port açık değil!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri okunurken hata oluştu: {ex.Message}");
            }
        }

        private float ConvertRegistersToFloat(ushort highOrderByte, ushort lowOrderByte)
        {
            // İki adet 16-bit register'ı birleştir ve float'a dönüştür
            ushort combinedValue = (ushort)((highOrderByte << 8) | lowOrderByte);

            // Örnek dönüşüm: register değerini doğrudan float'a dönüştür
            // Gereken dönüşüm işlemi yapılabilir
            return combinedValue / 10.0f; // Örneğin, sıcaklık değeri 10'a bölünerek float değeri elde ediliyor
        }

        private void timerReadData_Tick(object sender, EventArgs e)
        {
            // Her zamanlayıcı tetiklenmesinde veri oku
            ReadTemperatureData();
        }
        private void timerLogData_Tick(object sender, EventArgs e)
        {
            // Her saat başı tetiklendiğinde veri logla
            LogTemperatureData();
        }

        private void LogTemperatureData()
        {
            try
            {
                string filePath = "temperature_log.txt";
                string logEntry = $"{DateTime.Now}: {textBoxTemperature.Text}\n";

                File.AppendAllText(filePath, logEntry);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri loglanırken hata oluştu: {ex.Message}");
            }
        }
    }
}
