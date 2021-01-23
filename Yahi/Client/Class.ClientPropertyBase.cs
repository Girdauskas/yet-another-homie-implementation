﻿using System.ComponentModel;

namespace DevBot9.Protocols.Homie {
    public class ClientPropertyBase : PropertyBase {
        private string _name = "";
        public string Name {
            get { return _name; }
            protected set {
                _name = value;
                RaisePropertyChanged(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        private DataType _dataType = DataType.String;
        public DataType DataType {
            get { return _dataType; }
            protected set {
                _dataType = value;
                RaisePropertyChanged(this, new PropertyChangedEventArgs(nameof(Format)));
            }
        }

        private string _format = "";
        public string Format {
            get { return _format; }
            protected set {
                _format = value;
                RaisePropertyChanged(this, new PropertyChangedEventArgs(nameof(Format)));
            }
        }

        private string _unit = "";
        public string Unit {
            get { return _unit; }
            protected set {
                _unit = value;
                RaisePropertyChanged(this, new PropertyChangedEventArgs(nameof(Unit)));
            }
        }

        protected string _rawValue = "";

        protected ClientPropertyBase(string propertyId) {
            _propertyId = propertyId;
        }

        internal override void Initialize(Device parentDevice) {
            _parentDevice = parentDevice;

            _parentDevice.InternalPropertySubscribe($"{_propertyId}/$name", (value) => {
                Name = value;
            });

            _parentDevice.InternalPropertySubscribe($"{_propertyId}/$datatype", (value) => {
                DataType = DataType.String;
            });

            _parentDevice.InternalPropertySubscribe($"{_propertyId}/$format", (value) => {
                Format = value;
            });

            _parentDevice.InternalPropertySubscribe($"{_propertyId}/$unit", (value) => {
                Unit = value;
            });

            //_parentDevice.InternalPropertySubscribe($"{_propertyId}", payload => {
            //    RawValue = payload;
            //});
        }
    }
}
