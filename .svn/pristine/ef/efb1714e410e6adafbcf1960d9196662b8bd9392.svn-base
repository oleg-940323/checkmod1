using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace checkmod.ValidationRules
{
    public class ComparisonValue : DependencyObject
    {
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set {SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register( nameof(Value), typeof(string), 
            typeof(ComparisonValue), new PropertyMetadata(default(string)));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComparisonValue comparisonValue = (ComparisonValue)d;
            BindingExpressionBase bindingExpressionBase = BindingOperations.GetBindingExpressionBase(comparisonValue, BindingToTriggerProperty);
            bindingExpressionBase?.UpdateSource();
        }

        public object BindingToTrigger
        {
            get { return GetValue(BindingToTriggerProperty); }
            set { SetValue(BindingToTriggerProperty, value); }
        }

        public static readonly DependencyProperty BindingToTriggerProperty = DependencyProperty.Register(nameof(BindingToTrigger), typeof(object), 
            typeof(ComparisonValue),  new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}
