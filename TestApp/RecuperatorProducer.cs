﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DevBot9.Protocols.Homie;
using uPLibrary.Networking.M2Mqtt;

namespace TestApp {
    internal class RecuperatorProducer {
        private MqttClient _mqttClient;
        private string _mqttBrokerIp = "172.16.0.3";
        private string _mqttClientGuid = Guid.NewGuid().ToString();


        private HostDevice _hostDevice;
        private HostStateProperty _inletTemperature;
        private HostCommandProperty _turnOnOff;
        private HostParameterProperty _power;
        private HostStateProperty _actualPower;

        public RecuperatorProducer() {
            _mqttClient = new MqttClient(_mqttBrokerIp);
            _mqttClient.MqttMsgPublishReceived += HandlePublishReceived;
        }

        public void Initialize() {
            _mqttClient.Connect(_mqttClientGuid);


            _hostDevice = DeviceFactory.CreateHostDevice("temp", "recuperator", "Recuperator");
            _inletTemperature = _hostDevice.CreateHostStateProperty("inlet-temperature", "Inlet sensor", DataType.Float, "°C");
            _actualPower = _hostDevice.CreateHostStateProperty("actual-power", "Actual power", DataType.String, "%");
            _turnOnOff = _hostDevice.CreateHostCommandProperty("self-destruct", "On/off switch", DataType.String, "");
            _turnOnOff.PropertyChanged += (sender, e) => {
                Debug.WriteLine($"Beginning self-destruct in {_turnOnOff.Value}");
            };
            _power = _hostDevice.CreateHostParameterProperty("ventilation-power", "Ventilation power", DataType.String, "%");
            _power.PropertyChanged += (sender, e) => {
                Debug.WriteLine($"Ventilation power set to {_power.Value}");
                Task.Run(async () => {
                    _actualPower.SetValue("10");
                    await Task.Delay(1000);
                    _actualPower.SetValue("20");
                    await Task.Delay(1000);
                    _actualPower.SetValue("30");

                });
            };

            _hostDevice.Initialize((topic, value) => {
                _mqttClient.Publish(topic, Encoding.UTF8.GetBytes(value));

            }, topic => {
                _mqttClient.Subscribe(new string[] { topic }, new byte[] { 2 });
            });


            Task.Run(async () => {
                while (true) {
                    _inletTemperature.SetValue(new Random().Next(10, 30).ToString("F2"));
                    await Task.Delay(1000);
                }
            });
        }

        private void HandlePublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e) {
            _hostDevice.HandlePublishReceived(e.Topic, Encoding.UTF8.GetString(e.Message));
        }
    }
}
