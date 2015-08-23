	
CREATE proc dbo.TemplateGet
    @ID bigint
as
begin
	select	*
	from	Template
	where	ID = @ID
end