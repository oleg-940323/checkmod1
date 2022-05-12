using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;

namespace checkmod.ValidationRules
{
    public class ValidationRuleRange : ValidationRule
    {
        public ComparisonValue ComparisonValueMin { get; set; }
        public ComparisonValue ComparisonValueMax { get; set; }
        public ValueType type { get; set; }


        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (type.type_of_value == "INT")
            {
                string s = value?.ToString();
                Int16 number, min, max;

                if (!Int16.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!Int16.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!Int16.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "DINT")
            {
                string s = value?.ToString();
                Int32 number, min, max;

                if (!Int32.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!Int32.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!Int32.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "UINT")
            {
                string s = value?.ToString();
                UInt16 number, min, max;

                if (!UInt16.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt16.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt16.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "UDINT")
            {
                string s = value?.ToString();
                UInt32 number, min, max;

                if (!UInt32.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt32.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt32.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "BYTE")
            {
                string s = value?.ToString();
                byte number, min, max;

                if (!byte.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "USINT")
            {
                string s = value?.ToString();
                byte number, min, max;

                if (!byte.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "FLOAT")
            {
                string s = value?.ToString();
                float number, min, max;

                if (!float.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "STRUCT_TIME")
            {
                string s = value?.ToString();
                byte number, min, max;

                if (!byte.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "ERRORS_SIG_T2")
            {
                string s = value?.ToString();
                UInt32 number, min, max;

                if (!UInt32.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt32.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt32.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "ERRORS_SIG_T")
            {
                string s = value?.ToString();
                UInt32 number, min, max;

                if (!UInt32.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt32.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!UInt32.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "DSIGNAL_T")
            {
                string s = value?.ToString();
                byte number, min, max;

                if (!byte.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!byte.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "RMS_SIG_T")
            {
                string s = value?.ToString();
                float number, min, max;

                if (!float.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "TEMP_SIG_T")
            {
                string s = value?.ToString();
                float number, min, max;

                if (!float.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            else if (type.type_of_value == "INSTVAL_SIG_T")
            {
                string s = value?.ToString();
                float number, min, max;

                if (!float.TryParse(s, out number))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMin.Value, out min))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (!float.TryParse(ComparisonValueMax.Value, out max))
                {
                    return new ValidationResult(false, "Недействительная запись");
                }

                if (number < min || (number > max))
                {
                    return new ValidationResult(false, $"Значение выходит за диапазон: {min} - {max}");
                }
            }
            return ValidationResult.ValidResult;
        }
    }
}
