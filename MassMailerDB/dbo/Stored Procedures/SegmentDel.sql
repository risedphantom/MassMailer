	
CREATE proc [dbo].[SegmentDel]
    @ID int
as
begin
	begin tran
		delete  ClientSet
		where   SetID = @ID

		delete  Sets
		where   ID = @ID
    commit
end