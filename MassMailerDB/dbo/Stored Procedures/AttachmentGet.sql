	
CREATE proc dbo.AttachmentGet
    @AttachmentID bigint
as
begin
	select	Name
		,	Data
		,	Data.PathName() as FilePath
	from	Attachment
	where	ID = @AttachmentID
end