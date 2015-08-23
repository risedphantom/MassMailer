
CREATE function dbo.MissionStateGetBySysName
(
    @SysName varchar(8000)
)
returns int
as
begin
	declare	@ID int
	
	select	@ID = ID
	from	MissionState
	where	SysName = @SysName

	return @ID
end