﻿using System;
using DevBot9.Protocols.Homie;
using DevBot9.Protocols.Homie.Utilities;

namespace TestApp {
    internal class LightbulbConsumer {
        private PahoClientDeviceConnection _broker = new PahoClientDeviceConnection();

        private ClientDevice _clientDevice;
        private ClientColorProperty _color;

        public LightbulbConsumer() { }

        public void Initialize(string mqttBrokerIpAddress, AddToLogDelegate addToLog) {
            // Creating a air conditioner device.
            _clientDevice = DeviceFactory.CreateClientDevice("lightbulb");

            // Creating properties.          
            _color = _clientDevice.CreateClientColorProperty(new ClientPropertyMetadata { PropertyType = PropertyType.Parameter, NodeId = "general", PropertyId = "color", Format = "rgb" });
            _color.PropertyChanged += (sender, e) => {
                if (_color.Value.RedValue > 0) {
                    Console.WriteLine("Me no like red!");
                    _color.Value = HomieColor.FromRgbString("0,128,128");
                }
            };

            // Initializing all the Homie stuff.
            _broker.Initialize(mqttBrokerIpAddress, (severity, message) => addToLog(severity, "Broker:" + message));
            _clientDevice.Initialize(_broker, (severity, message) => addToLog(severity, "ClientDevice:" + message));
        }
    }
}
