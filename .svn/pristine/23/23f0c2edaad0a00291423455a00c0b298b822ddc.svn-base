using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace checkmod.ValidationRules
{
    public class ValueType : DependencyObject
    {
        public string type_of_value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(type_of_value), typeof(string),
            typeof(ValueType), new PropertyMetadata(default(string)));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ValueType comparisonValue = (ValueType)d;
            BindingExpressionBase bindingExpressionBase = BindingOperations.GetBindingExpressionBase(comparisonValue, BindingToTriggerProperty);
            bindingExpressionBase?.UpdateSource();
        }

        public object BindingToTrigger
        {
            get { return GetValue(BindingToTriggerProperty); }
            set { SetValue(BindingToTriggerProperty, value); }
        }

        public static readonly DependencyProperty BindingToTriggerProperty = DependencyProperty.Register(nameof(BindingToTrigger), typeof(object),
            typeof(ValueType), new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}
