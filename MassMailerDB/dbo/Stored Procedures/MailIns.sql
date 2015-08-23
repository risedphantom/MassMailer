
CREATE proc [dbo].[MailIns]
    @DefaultMailGroupID int,
    @Sender varchar(250),
    @Name varchar(250),
    @Subject varchar(250),
    @Body text,
    @BodyText text,
    @ShowInClientNotification int,
    @UserName varchar(250),
	@EmailTo varchar(250) = null,
	@BranchID int = 1,
	@SendForNotSpam int = null
as
begin
	set xact_abort on

	declare @ID bigint
		,	@Priority int

	select	@Priority = ID
	from	Priority
	where	Name = 'WeeklyMail'

	begin tran
		exec TemplateAdd @ID = @ID out, @Name = @Name, @Description = null, @Body = @Body

		exec MailingAdd @ID = null, @AddressFrom = @Sender, @Name = @Name, @Subject = @Subject, @TemplateID = @ID, @Priority = @Priority	
    commit
end