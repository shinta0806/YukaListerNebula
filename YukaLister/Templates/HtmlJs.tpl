// ----------------------------------------------------------------------------
// 全ての番組名を開く・閉じる
// ----------------------------------------------------------------------------

function set_all_programs(isOpen) {
	var parents = document.getElementsByClassName('accparent');
	for (var i = 0; i < parents.length; i++) {
		parents[i].checked = isOpen;
	}
	return false;
}
