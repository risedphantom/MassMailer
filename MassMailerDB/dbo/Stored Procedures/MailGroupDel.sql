CREATE proc [dbo].[MailGroupDel]
    @ID int
as
begin
    begin tran
		delete  MailGroupUser
		where   MailGroupID = @ID

		delete  MailGroup
		where   ID = @ID
    commit
end