// Serilog's static Log.Logger is (re)assigned every time a host starts, so two in-memory hosts
// running concurrently would steal each other's log sink. Host the API one test class at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
