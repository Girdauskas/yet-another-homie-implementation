﻿using System;

namespace DevBot9.Protocols.Homie {
    public class HostDateTimeProperty : HostPropertyBase {
        private string _isoFormatString = "yyyy-MM-ddTHH:mm:ss.fff";

        public DateTime Value {
            get {
                var returnValue = DateTime.Parse(_rawValue);

                return returnValue;
            }
            set {
                SetValue(value);
            }
        }

        internal HostDateTimeProperty(PropertyType propertyType, string propertyId, string friendlyName) : base(propertyType, propertyId, friendlyName, DataType.DateTime, "", "") {
            _rawValue = new DateTime(2000, 01, 01).ToString(_isoFormatString);
        }

        internal override void Initialize(Device parentDevice) {
            base.Initialize(parentDevice);
        }

        protected override bool ValidatePayload(string payloadToValidate) {
            var isPayloadGood = false;

            isPayloadGood = DateTime.TryParse(payloadToValidate, out var parsedDateTime);

            return isPayloadGood;
        }

        private void SetValue(DateTime valueToSet) {
            switch (Type) {
                case PropertyType.State:
                case PropertyType.Parameter:
                    _rawValue = valueToSet.ToString(_isoFormatString);
                    _parentDevice.InternalPropertyPublish($"{_propertyId}", _rawValue);

                    break;

                case PropertyType.Command:
                    throw new InvalidOperationException();
            }
        }
    }
}
