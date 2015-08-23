
CREATE proc [dbo].[MailGroupUserDel]
    @ID int
as
begin
    delete  MailGroupUser
    where   ID = @ID
end