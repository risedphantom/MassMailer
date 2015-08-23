
CREATE proc dbo.TemplateAdd
    @ID bigint out,
	@Name varchar(8000),
    @Description varchar(8000),
	@Body text,
	@IsHTML bit = 1
as
begin
    insert Template(Name, Description, Body, ChangeMoment, IsHTML)
    values (@Name, @Description, @Body, Getdate(), @IsHTML)

	set @ID = SCOPE_IDENTITY()
end