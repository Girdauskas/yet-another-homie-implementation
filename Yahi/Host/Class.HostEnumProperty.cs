﻿using System;
using System.Linq;

namespace DevBot9.Protocols.Homie {
    public class HostEnumProperty : HostPropertyBase {
        public string Value {
            get {
                var returnValue = _rawValue;

                return returnValue;
            }
            set {
                SetValue(value);
            }
        }

        internal HostEnumProperty(PropertyType propertyType, string propertyId, string friendlyName, in string[] possibleValues) : base(propertyType, propertyId, friendlyName, DataType.Enum, "option1,option2", "") {
            if (possibleValues.Length == 0) { throw new ArgumentException("Please provide at least one correct value for this property", nameof(possibleValues)); }

            var localFormat = possibleValues[0];
            for (var i = 1; i < possibleValues.Length; i++) {
                localFormat += "," + possibleValues[i];
            }
            _formatAttribute = localFormat;
            _rawValue = possibleValues[0];
        }

        internal override void Initialize(Device parentDevice) {
            base.Initialize(parentDevice);
        }

        protected override bool ValidatePayload(string payloadToValidate) {
            var isPayloadGood = false;
            var enumParts = _formatAttribute.Split(',').ToList();

            if (enumParts.Any(e => e == payloadToValidate)) { isPayloadGood = true; }

            return isPayloadGood;
        }

        private void SetValue(string valueToSet) {
            switch (Type) {
                case PropertyType.State:
                case PropertyType.Parameter:
                    if (ValidatePayload(valueToSet) == true) {
                        _rawValue = valueToSet;
                        _parentDevice.InternalPropertyPublish($"{_propertyId}", _rawValue);
                    }

                    break;

                case PropertyType.Command:
                    throw new InvalidOperationException();
            }
        }
    }
}
