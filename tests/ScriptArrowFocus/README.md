# Map arrow navigation consistency

Actual MainWindow/MapTab controls with synthetic object, green-event and map-header
script data. The baseline fails when an object arrow opens the correct script but
leaves keyboard focus outside its source. Later assertions require the same source
focus for green events, regular map headers and conditional child scripts, retain
condition-table navigation for conditional parents, avoid selecting all source,
and disable/re-enable a green arrow as its address becomes invalid/valid.

Build the project and run its DLL. Signpost's intentionally different select-all
arrow behavior remains covered by SignpostScriptNavigation; Map View does not use
these arrow callbacks. No personal ROM or settings are opened.
