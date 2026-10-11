# Inventory

- lists-products-below-the-threshold: Asking for the products below a threshold returns every
  product whose available quantity is lower than that threshold.
Test: DcaShop.IntegrationTests.LowStockOverviewTest#NamesEveryProductHoldingLessThanTheGivenQuantity

- excludes-products-at-or-above-the-threshold: A product whose available quantity equals or exceeds
  the threshold is not part of the answer.
Test: DcaShop.IntegrationTests.LowStockOverviewTest#LeavesOutProductsHoldingTheGivenQuantityOrMore

- names-the-available-quantity: Each product in the answer names its available quantity, so the
  operator can tell how urgent it is.
Test: DcaShop.IntegrationTests.LowStockOverviewTest#StatesHowMuchIsLeftOfEachProductItNames

- empty-answer-when-nothing-is-low: When no product is below the threshold, the answer is empty and
  not an error.
Test: DcaShop.IntegrationTests.LowStockOverviewTest#AnswersWithAnEmptyListWhenNoProductIsShort
