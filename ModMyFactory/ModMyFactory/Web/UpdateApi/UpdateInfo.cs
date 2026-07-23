namespace ModMyFactory.Web.UpdateApi
{
    sealed class UpdateInfo
    {
        public Package Package { get; }

        public UpdateInfo(UpdateStepTemplate[] templates)
        {
            Package = new Package(templates);
        }
    }
}
