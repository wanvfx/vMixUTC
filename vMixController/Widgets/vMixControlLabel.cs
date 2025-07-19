namespace vMixController.Widgets
{
    public class vMixControlLabel: vMixControl
    {

        public override bool IsCaptionOn { get => true; set => base.IsCaptionOn = true; }

        public override string Type
        {
            get
            {
                return Extensions.LocalizationManager.Get("Label");
            }
        }
    }
}
