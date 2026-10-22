Feature: Rice Cultivation B initial loaded-game acceptance

  @quickstart:RiceQuickstart @timeout:240
  Scenario: Pawn construction, Waterworks sources, Vanilla zones and real save load
    Given a pawn constructs a rice paddy and zone interactions preserve their owners
    When I save and reload
    Then rice plots, source water and growing zones survive the real reload
