﻿using System;
using System.Diagnostics;
using DevBot9.Protocols.Homie;
using DevBot9.Protocols.Homie.Utilities;
using Windows.Devices.Pwm;

namespace TestApp {
    internal class LightbulbProducer {
        private IMqttBroker _broker;
        private readonly string _mqttClientGuid = Guid.NewGuid().ToString();

        private HostDevice _hostDevice;
        private HostChoiceProperty _onOffSwitch;
        private HostColorProperty _color;
        private HostNumberProperty _intensity;

        private double _cachedRed = 0;
        private double _cachedGreen = 0;
        private double _cachedBlue = 0;

        private PwmController _redController = PwmController.FromId("TIM0");
        private PwmController _greenController = PwmController.FromId("TIM1");
        private PwmController _blueController = PwmController.FromId("TIM2");

        private PwmPin _redPin;
        private PwmPin _greenPin;
        private PwmPin _bluePin;

        public LightbulbProducer() { }

        public void Initialize(string mqttBrokerIpAddress) {
            // Initializing RGB pins and timers.
            _redController.SetDesiredFrequency(10000);
            _greenController.SetDesiredFrequency(10000);
            _blueController.SetDesiredFrequency(10000);

            _redPin = _redController.OpenPin(0);
            _greenPin = _greenController.OpenPin(2);
            _bluePin = _blueController.OpenPin(4);

            _redPin.SetActiveDutyCyclePercentage(0.000f);
            _greenPin.SetActiveDutyCyclePercentage(0.000f);
            _bluePin.SetActiveDutyCyclePercentage(0.000f);

            _redPin.Start();
            _greenPin.Start();
            _bluePin.Start();

            // Connecting to broker.
            _broker = new PahoBroker();
            _broker.Initialize(mqttBrokerIpAddress);

            // Creating devices and properties.
            _hostDevice = DeviceFactory.CreateHostDevice("lightbulb", "Colorful lightbulb");

            #region General node

            _hostDevice.UpdateNodeInfo("general", "General information and properties", "no-type");

            // I think properties are pretty much self-explanatory in this producer.
            _color = _hostDevice.CreateHostColorProperty(PropertyType.Parameter, "general", "color", "Color", ColorFormat.Rgb);
            _color.PropertyChanged += (sender, e) => {
                _cachedRed = _color.Value.RedValue;
                _cachedGreen = _color.Value.GreenValue;
                _cachedBlue = _color.Value.BlueValue;
                UpdateLed();

                Debug.WriteLine($"Color changed to {_color.Value.ToRgbString()}");
            };
            _onOffSwitch = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "general", "is-on", "Is on", new[] { "ON,OFF" });
            _onOffSwitch.PropertyChanged += (sender, e) => {
                // Simulating some lamp behaviour.
                if (_onOffSwitch.Value == "ON") {
                    _intensity.Value = 100;
                }
                else {
                    _intensity.Value = 0;
                }
                UpdateLed();
            };

            _intensity = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "general", "intensity", "Intensity", 0, "%");
            _intensity.PropertyChanged += (sender, e) => {
                UpdateLed();

                Debug.WriteLine($"Intensity changed to {_intensity.Value}");
            };

            #endregion

            _hostDevice.Initialize(_broker);
        }

        private void UpdateLed() {
            var intensity = _intensity.Value / 100.0f;

            var redCycle = _cachedRed / 255 * intensity;
            var greenCycle = _cachedGreen / 255 * intensity;
            var blueCycle = _cachedBlue / 255 * intensity;

            _redPin.SetActiveDutyCyclePercentage(redCycle);
            _greenPin.SetActiveDutyCyclePercentage(greenCycle);
            _bluePin.SetActiveDutyCyclePercentage(blueCycle);
        }
    }
}
