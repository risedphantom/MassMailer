
CREATE proc [dbo].[TemplateUpd]
    @ID bigint,
	@Name varchar(8000),
    @Description varchar(8000),
	@Body text
as
begin
    update	Template
	set		Name = @Name
		,	Description = @Description
		,	Body = @Body
		,	ChangeMoment = GetDate()
		,	GUID = newid()
	where	ID = @ID
end