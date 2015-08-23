
CREATE proc dbo.MailingAdd
	@ID bigint out,
    @AddressFrom varchar(250),
    @Name varchar(250),
    @Subject varchar(250),
	@TemplateID bigint,
	@Priority int
as
begin
    insert Mailing(MailStateID, TemplateID, StateChangeMoment, AddressFrom, Name, Subject, Priority)
    values (1, @TemplateID, Getdate(), @AddressFrom, @Name, @Subject, @Priority)

	set @ID = SCOPE_IDENTITY()
end