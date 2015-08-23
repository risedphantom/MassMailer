
CREATE proc [dbo].[MailGroupUserUpd]
	@ID int,
    @MailGroupID int,
    @Email varchar(250),
    @FIO varchar(250),
	@ClientID bigint = null
as
begin
    update	MailGroupUser
	set		MailGroupID = @MailGroupID, 
			Email = @Email, 
			FIO = @FIO, 
			ClientID = @ClientID
	where	ID = @ID
end