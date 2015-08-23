
CREATE proc dbo.MailingUpd
	@ID bigint,
    @AddressFrom varchar(250),
    @Name varchar(250),
    @Subject varchar(250),
	@TemplateID bigint,
	@Priority int
as
begin
    update	Mailing
	set		TemplateID = @TemplateID
		,	StateChangeMoment = GetDate()
		,	AddressFrom = @AddressFrom
		,	Name = @Name
		,	Subject = @Subject
		,	Priority = @Priority
	where	ID = @ID
end