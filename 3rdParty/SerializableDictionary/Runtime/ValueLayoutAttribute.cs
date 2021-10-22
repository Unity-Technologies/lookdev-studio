[System.AttributeUsage(System.AttributeTargets.Field)]
public class ValueLayoutAttribute : System.Attribute {
    public string keyLabel, value1Label, value2Label, value3Label, value4Label;
    // Widths are only for the data field, the label is auto-sized.
    public float  keyWidth, value1Width, value2Width, value3Width, value4Width;

    public string GetLabel (int index) {
#if UNITY_2020_2_OR_NEWER
        return index switch {
            0 => keyLabel,
            1 => value1Label,
            2 => value2Label,
            3 => value3Label,
            4 => value4Label,
            _ => ""
        };
#else
        switch (index) {
            case 0:
                return keyLabel;
            case 1:
                return value1Label;
            case 2:
                return value2Label;
            case 3:
                return value3Label;
            case 4:
                return value4Label;
            default:
                return "";
        }
#endif
    }

    public float GetWidth (int index) {
#if UNITY_2020_2_OR_NEWER
        return index switch {
            0 => keyWidth,
            1 => value1Width,
            2 => value2Width,
            3 => value3Width,
            4 => value4Width,
            _ => 0f
        };
#else
        switch (index) {
            case 0:
                return keyWidth;
            case 1:
                return value1Width;
            case 2:
                return value2Width;
            case 3:
                return value3Width;
            case 4:
                return value4Width;
            default:
                return 0f;
        }
#endif
    }

    public ValueLayoutAttribute () {
        keyLabel = value1Label = value2Label = value3Label = value4Label = string.Empty;
        keyWidth = value1Width = value2Width = value3Width = value4Width = 0f;
    }
}
