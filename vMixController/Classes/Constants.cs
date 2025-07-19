using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vMixController.Classes
{
    public static class Constants
    {
        public const string BUTTON_STYLE_MOMENTARY = "Momentary";
        public const string BUTTON_STYLE_TOGGLE = "Toggle";
        public const string BUTTON_STYLE_PRESS = "Press";
        public static string[] ButtonStyle { get; } = new string[] { BUTTON_STYLE_MOMENTARY, BUTTON_STYLE_TOGGLE };

        public const string BUTTON_IMAGE_TYPE_DEFAULT = "Default";
        public const string BUTTON_IMAGE_TYPE_DEFAULTPRESSED = "Default+Pressed";
        public static string[] ButtonImageType { get; } = new string[] { BUTTON_IMAGE_TYPE_DEFAULT, BUTTON_IMAGE_TYPE_DEFAULTPRESSED };

        public const string TEXTFIELD_STYLE_FILE = "File";
        public const string TEXTFIELD_STYLE_TEXT = "Text";

        public static string[] TextFieldStyle { get; } = new string[] { TEXTFIELD_STYLE_FILE, TEXTFIELD_STYLE_TEXT };

        public const string SCORE_BASIC = "Basic";
        public const string SCORE_BASKETBALL = "Basketball";
        public const string SCORE_RUGBY = "Rugby";
        public const string SCORE_AMERICANFOOTBALL = "American Football";
        public const string SCORE_CUSTOM = "Custom";
        public static string[] ScoreStyle { get; } = new string[] { SCORE_BASIC, SCORE_BASKETBALL, SCORE_RUGBY, SCORE_AMERICANFOOTBALL, SCORE_CUSTOM };

        public const string HORIZONTAL = "Horizontal";
        public const string VERTICAL = "Vertical";
        public static string[] HorizontalVertical { get; } = new string[] { HORIZONTAL, VERTICAL };

        public const string TBAR_MODE_AB = "A/B";
        public const string TBAR_MODE_SNAPBACK = "Snap Back";
        public static string[] TBarMode {  get; } = new string[] { TBAR_MODE_AB, TBAR_MODE_SNAPBACK };
    }
}