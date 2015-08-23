
CREATE proc [dbo].[MailGroupUserLst]
    @MailGroupID int
as
begin
    select  *
    from    MailGroupUser with(nolock)
    where   MailGroupID = @MailGroupID
    order by Email
end