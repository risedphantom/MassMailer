
CREATE proc [dbo].[MailGroupUserGet]
    @ID int
as
begin
    select  *
    from    MailGroupUser with(nolock)
    where   ID = @ID
end