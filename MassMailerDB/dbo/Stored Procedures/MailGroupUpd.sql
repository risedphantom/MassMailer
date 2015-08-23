
CREATE proc [dbo].[MailGroupUpd]
    @ID int,
    @Name varchar(250)
as
begin
    update  MailGroup
    set     Name = @Name
    where   ID = @ID
end