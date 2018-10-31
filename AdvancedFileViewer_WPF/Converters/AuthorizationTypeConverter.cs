using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AdvancedFileViewer_WPF.Converters
{
    class AuthorizationTypeConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter.ToString();
            var isNewUser = System.Convert.ToBoolean(value);
            if (param == "LoginButton")
            {
                if (isNewUser) return "Sign Up";
                else return "Sign In";
            }
            if (param == "LoginLabel")
            {
                if (!isNewUser) return "Don't Have an Account?";
                else return "Already have an account? Sign in";
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
