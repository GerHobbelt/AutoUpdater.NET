namespace AutoUpdaterDotNET.BasicImpls
{
    internal class BasicUpdateFormPresenterFactory : UpdateFormPresenterFactory
    {
        public UpdateFormPresenter Create() => new UpdateForm();
    }


}