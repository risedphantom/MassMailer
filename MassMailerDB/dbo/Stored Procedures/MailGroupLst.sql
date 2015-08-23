
CREATE proc [dbo].[MailGroupLst]
as
begin
    select  *
    from    MailGroup with(nolock)
end