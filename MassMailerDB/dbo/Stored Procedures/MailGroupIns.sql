
CREATE proc [dbo].[MailGroupIns]
    @Name varchar(250)
as
begin
    insert MailGroup(Name)
    values (@Name)
    select ID = SCOPE_IDENTITY()
end