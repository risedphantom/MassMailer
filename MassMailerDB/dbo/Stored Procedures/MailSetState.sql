
CREATE proc dbo.MailSetState
    @ID int,
    @MailStateID int,
    @UserName varchar(250),
	@SelectID int = null,
	@SelectedQty int = null
	
as
begin
	update  Mailing
	set     MailStateID = @MailStateID,
			StateChangeMoment = GetDate()
	where   ID = @ID
end