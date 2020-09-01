namespace Hive.Logic

module Lazy =
    let force (l:Lazy<_>) = l.Force()
