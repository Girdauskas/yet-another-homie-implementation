﻿using System.Threading;
namespace DevBot9.Protocols.Homie {
    public class HostDevice : Device {
        internal HostDevice(string baseTopic, string id, string friendlyName = "") {
            _baseTopic = baseTopic;
            _deviceId = id;
            Name = friendlyName;
            State = States.Init;
        }

        public HostIntegerProperty CreateHostIntegerProperty(PropertyType propertyType, string propertyId, string friendlyName, string unit = "") {
            var createdProperty = new HostIntegerProperty(propertyType, propertyId, friendlyName, DataType.Integer, "", unit);

            _properties.Add(createdProperty);

            return createdProperty;
        }

        public HostFloatProperty CreateHostFloatProperty(PropertyType propertyType, string propertyId, string friendlyName, string unit = "") {
            var createdProperty = new HostFloatProperty(propertyType, propertyId, friendlyName, DataType.Float, "", unit);

            _properties.Add(createdProperty);

            return createdProperty;
        }

        public HostStringProperty CreateHostStringProperty(PropertyType propertyType, string propertyId, string friendlyName, string unit = "") {
            var createdProperty = new HostStringProperty(propertyType, propertyId, friendlyName, DataType.String, "", unit);

            _properties.Add(createdProperty);

            return createdProperty;
        }

        public HostBooleanProperty CreateHostBooleanProperty(PropertyType propertyType, string propertyId, string friendlyName) {
            var createdProperty = new HostBooleanProperty(propertyType, propertyId, friendlyName, DataType.Boolean, "", "");

            _properties.Add(createdProperty);

            return createdProperty;
        }

        public new void Initialize(PublishToTopicDelegate publishToTopicDelegate, SubscribeToTopicDelegate subscribeToTopicDelegate) {
            base.Initialize(publishToTopicDelegate, subscribeToTopicDelegate);

            SetState(States.Init);

            _publishToTopicDelegate($"{_baseTopic}/{_deviceId}/$homie", HomieVersion);
            _publishToTopicDelegate($"{_baseTopic}/{_deviceId}/$name", Name);
            //_client.Publish($"homie/{_deviceId}/$nodes", GetNodesString());
            //_client.Publish($"homie/{_deviceId}/$extensions", GetExtensionsString());

            // imitating some initialization work.
            Thread.Sleep(1000);

            SetState(States.Ready);
        }

        public void SetState(string stateToSet) {
            State = stateToSet;
            _publishToTopicDelegate($"{_baseTopic}/{_deviceId}/$state", State);
        }
    }
}
