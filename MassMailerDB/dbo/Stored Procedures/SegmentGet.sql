	
CREATE proc [dbo].[SegmentGet]
    @ID int
as
begin
	select	*
	from	Sets
	where	ID = @ID
end