
CREATE proc [dbo].[SetAdd]
    @Name varchar(250),
    @Description varchar(250),
	@SetID int output
as
begin
	insert into Sets(
			Name
		,	Description
		,	Date)
	values	(
			@Name
		,	@Description
		,	GETDATE())
	
	set @SetID = SCOPE_IDENTITY()
end