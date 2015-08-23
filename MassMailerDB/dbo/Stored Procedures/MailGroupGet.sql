
CREATE proc [dbo].[MailGroupGet]
    @ID int
as
begin
    select  *
    from    MailGroup with(nolock)
    where   ID = @ID
end