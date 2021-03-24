﻿using System;
using System.Globalization;

namespace DevBot9.Protocols.Homie {
    /// <summary>
    /// A property of type Float, as defined by the Homie convention.
    /// </summary>
    public class HostFloatProperty : HostPropertyBase {
        /// <summary>
        /// Set value will be published to the MQTT broker. Getting the property will retrieve value from the cache.
        /// </summary>
        public float Value {
            get {
                float returnValue;

                returnValue = float.Parse(_rawValue, CultureInfo.InvariantCulture);

                return returnValue;
            }
            set {
                SetValue(value);
            }
        }

        internal HostFloatProperty(PropertyType propertyType, string propertyId, string friendlyName, float initialValue, string format, string unit) : base(propertyType, propertyId, friendlyName, DataType.Float, format, unit) {
            _rawValue = initialValue.ToString("0.0#", CultureInfo.InvariantCulture);
        }

        internal override void Initialize(Device parentDevice) {
            base.Initialize(parentDevice);
        }

        protected override bool ValidatePayload(string payloadToValidate) {
            var returnValue = float.TryParse(payloadToValidate, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

            return returnValue;
        }

        private void SetValue(float valueToSet) {
            switch (Type) {
                case PropertyType.State:
                case PropertyType.Parameter:

                    _rawValue = valueToSet.ToString("0.0#", CultureInfo.InvariantCulture);

                    _parentDevice.InternalPropertyPublish($"{_propertyId}", _rawValue);
                    break;

                case PropertyType.Command:
                    throw new InvalidOperationException();
            }
        }
    }
}
