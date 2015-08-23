
CREATE proc dbo.MailAddInActiveQueue
	@AddressFrom varchar(250),
	@AddressTo varchar(250),
	@AddressCC varchar(250),
    @Subject varchar(250),
	@TemplateName varchar(250),
    @Body text,
	@IsHTML bit = 1,
	@TozonOwnerID bigint,
	@XML xml,
	@Priority int,
	@HasAttachment bit,
	@MessageID bigint out
as
begin
	declare @TemplateID bigint
		,	@WaitingStateID int = 0

	begin tran
		exec TemplateAdd @ID = @TemplateID out, @Name = @TemplateName, @Description = null, @Body = @Body, @IsHTML = @IsHTML

		insert	ActiveQueue (TemplateID, XMLData, AddressFrom, AddressTo, AddressCC, Subject, Priority, Status, MissionID, ExternalOwnerID, SendMoment, AddMoment, Host, HasAttachment)
		values (@TemplateID, @XML, @AddressFrom, @AddressTo, @AddressCC, @Subject, @Priority, @WaitingStateID, null, @TozonOwnerID, null, GetDate(), null, @HasAttachment)

		set @MessageID = scope_identity()
	commit
end