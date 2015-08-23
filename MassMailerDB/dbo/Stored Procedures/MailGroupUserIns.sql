
CREATE proc [dbo].[MailGroupUserIns]
    @MailGroupID int,
    @Email varchar(250),
    @FIO varchar(250),
	@ClientID int = null
as
begin
    insert MailGroupUser(MailGroupID, Email, FIO, ClientID)
    values (@MailGroupID, @Email, @FIO, @ClientID)
    select ID = SCOPE_IDENTITY()
end