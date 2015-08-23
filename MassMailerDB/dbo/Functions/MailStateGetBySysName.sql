
CREATE function dbo.MailStateGetBySysName
(
    @SysName varchar(8000)
)
returns int
as
begin
	declare	@ID int
	
	select	@ID = ID
	from	MailState
	where	SysName = @SysName

	return @ID
end