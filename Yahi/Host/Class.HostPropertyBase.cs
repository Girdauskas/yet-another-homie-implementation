﻿using System.ComponentModel;

namespace DevBot.Homie {
    public class HostPropertyBase {
        protected readonly string _nameAttribute = "";
        readonly DataType _dataTypeAttribute = DataType.String;
        readonly string _formatAttribute = "";
        readonly bool _isSettableAttribute = true;
        readonly bool _isRetainedAttribute = false;
        readonly string _unitAttribute;
        readonly string _nameAttributeTopic;
        readonly string _dataTypeAttributeTopic;
        readonly string _formatAttributeAttributeTopic;
        readonly string _isSettableAttributeTopic;
        readonly string _isRetainedAttributeTopic;
        readonly string _unitAttributeTopic;
        readonly string _valueTopic;

        public string Value { get; private set; }

        readonly string _propertyId;
        readonly string _topicPrefix;

        IBroker _broker;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected HostPropertyBase(string topicPrefix, string propertyId, string friendlyName, DataType dataType, string unit) {
            _topicPrefix = topicPrefix;
            _propertyId = propertyId;
            _nameAttribute = friendlyName;
            _dataTypeAttribute = dataType;
            _unitAttribute = unit;

            _nameAttributeTopic = $"{_topicPrefix}/{_propertyId}/$name";
            _dataTypeAttributeTopic = $"{_topicPrefix}/{_propertyId}/$datatype";
            _formatAttributeAttributeTopic = $"{_topicPrefix}/{_propertyId}/$format";
            _isSettableAttributeTopic = $"{_topicPrefix}/{_propertyId}/$settable";
            _isRetainedAttributeTopic = $"{_topicPrefix}/{_propertyId}/$retained";
            _unitAttributeTopic = $"{_topicPrefix}/{_propertyId}/$unit";

            _valueTopic = $"{_topicPrefix}/{_propertyId}";
        }
    }
}
