
CREATE proc dbo.SendMailForCRM
	@AddressFrom varchar(250),
	@AddressTo varchar(250),
	@AddressCC varchar(250),
    @Subject varchar(250),
    @Body text,
	@ExternalOwnerID bigint,
	@HasAttachment bit,
	@MailID bigint out	
as
begin
	declare @TemplateName varchar(250) = '[CRM] - ' + @Subject 
		,	@XML xml
		,	@Priority int = 1000 --Сервисные письма
	
	exec	MailAddInActiveQueue
			@AddressFrom = @AddressFrom
		,	@AddressTo = @AddressTo
		,	@AddressCC = @AddressCC
		,	@Subject = @Subject
		,	@TemplateName = @TemplateName
		,	@Body = @Body
		,	@IsHTML = 0
		,	@ExternalOwnerID = @ExternalOwnerID
		,	@XML = null
		,	@Priority = @Priority
		,	@HasAttachment = @HasAttachment
		,	@MessageID = @MailID out
end