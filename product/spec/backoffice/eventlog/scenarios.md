# backoffice — eventlog

### Rule: One entry per event and listener, with its state and dates

#### entry-shows-listener-state-and-dates
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then every entry shows a badge "Completed" or "Incomplete", a "Listener" and a "Published" date in the form yyyy-MM-dd HH:mm:ss
- And every entry with the badge "Completed" also shows a "Completed" date in the same form
Test: DcaShop.IntegrationTests.BackofficeEventLogPageTest#EveryEntryShowsItsBadgeListenerAndDates

#### newest-entry-comes-first
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then the entries are ordered by their "Published" date, the newest first
Test: DcaShop.IntegrationTests.EventLogOrderTest#TheNewestEntryComesFirst

### Rule: The summary counts the entries

#### counts-add-up
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then "Total Events" equals the number of entries listed
- And "Completed" is the number of entries with the badge "Completed", "Incomplete" the number with the badge "Incomplete", and both add up to "Total Events"
Test: DcaShop.IntegrationTests.BackofficeEventLogPageTest#TheSummaryCountsTheEntriesByTheirBadge

### Rule: The shop's start-up already shows in the log

#### log-lists-the-events-published-at-start-up
- Given the shop has started and seeded the sample catalogue, and the operator is logged in
- When they open `/backoffice/events`
- Then the log lists at least one entry
- And every entry shows a type label that names the event
Test: DcaShop.E2eTests.BackofficeE2eTest#EventLogShowsPublishedEvents

### Rule: The page is recognisable and can be reloaded

#### log-page-breadcrumb
- Given the operator is logged in
- When they open `/backoffice/events`
- Then the breadcrumb reads "Home / Event Publication Log" and "Home" links to `/`
Test: DcaShop.IntegrationTests.BackofficeEventLogPageTest#TheBreadcrumbLeadsHomeFromTheEventPublicationLog

#### refresh-reloads-the-log
- Given the operator is logged in on `/backoffice/events`
- When they press "Refresh"
- Then `/backoffice/events` is loaded again with its summary and entries
Test: DcaShop.IntegrationTests.BackofficeEventLogPageTest#RefreshLoadsTheLogAgainWithItsSummaryAndEntries
