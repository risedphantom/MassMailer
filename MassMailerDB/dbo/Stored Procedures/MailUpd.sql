
CREATE proc dbo.MailUpd
    @ID int,
    @DefaultMailGroupID int,
    @Sender varchar(250),
    @Name varchar(250),
    @Subject varchar(250),
    @Body text,
    @BodyText text,
	@ShowInClientNotification int = 1,
	@EmailTo varchar(250) = null,
	@SendForNotSpam int = null
as
begin
	set xact_abort on

	declare @TemplateID bigint
		,	@Priority int

	select	@Priority = ID
	from	Priority
	where	Name = 'WeeklyMail'

	select	@TemplateID = TemplateID
	from	Mailing
	where	ID = @ID

	begin tran
		exec TemplateUpd @ID = @TemplateID, @Name = @Name, @Description = null, @Body = @Body

		exec MailingUpd @ID = @ID, @AddressFrom = @Sender, @Name = @Name, @Subject = @Subject, @TemplateID = @TemplateID, @Priority = @Priority	
    commit
end